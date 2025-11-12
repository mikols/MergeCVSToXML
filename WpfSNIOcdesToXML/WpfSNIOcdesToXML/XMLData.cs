using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace WpfSNIOcdesToXML
{
    public class XMLData
    {
        private string xmlFile = "";
        private string csvFile = "";
        public string ResultFile = string.Empty;
        public string ChangeReportFile = string.Empty;


        public string XmlFIle { get => xmlFile; set => xmlFile = value; }
        public string CsvFIle { get => csvFile; set => csvFile = value; }


        public List<string> UpdatedNodes = new List<string>();
        public List<string> AddedNodes = new List<string>();
        public List<string> FailedAdditions = new List<string>();


        public bool RUN()
        {
            ResultFile = string.Empty;
            if (File.Exists(xmlFile) && File.Exists(csvFile))
            {
                // Hämta katalogen där XML-filen ligger
                string xmlDirectory = Path.GetDirectoryName(Path.GetFullPath(XmlFIle));
                //ResultFile = Path.Combine(xmlDirectory, $"updated.XML.{DateTime.Now:yyyyMMdd_HHmm}.xml");
                ResultFile = Path.Combine(xmlDirectory, $"updated.XML.xml");
                //ChangeReportFile = Path.Combine(xmlDirectory, $"changeReport.{DateTime.Now:yyyyMMdd_HHmm}.txt");
                ChangeReportFile = Path.Combine(xmlDirectory, $"changeReport.txt");
                updateAndSaveXml_v2(XmlFIle, CsvFIle);
                CreateChangeReport();
                return true;
            }
            return false;
        }

        public void updateAndSaveXml(string xmlPath, string csvPath)
        {

            // 1. Läs in XML
            XDocument doc = XDocument.Load(xmlPath);

            // 2. Läs in CSV till dictionary
            var updates = File.ReadAllLines(csvPath)
                              .Where(line => !string.IsNullOrWhiteSpace(line))
                              .Select(line => line.Split(';'))
                              .Where(parts => parts.Length >= 2)
                              .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());

            // 3. Hitta alla befintliga item-noder
            var allItems = doc.Descendants("item").ToList();
            var existingIds = new HashSet<string>(allItems.Select(i => i.Attribute("id")?.Value));

            // 4. Uppdatera befintliga noder
            foreach (var item in allItems)
            {
                string id = item.Attribute("id")?.Value;
                if (id != null && updates.ContainsKey(id))
                {
                    item.SetAttributeValue("text", updates[id]);
                    updates.Remove(id); // Ta bort så vi vet vilka som är nya
                }
            }

            // 5. Lägg till nya noder för kvarvarande uppdateringar
            foreach (var kvp in updates)
            {
                string newId = kvp.Key;
                string newText = kvp.Value;

                // Gissa förälder baserat på ID-struktur (t.ex. "0113" är förälder till "01131")
                string parentId = newId.Length > 1 ? newId.Substring(0, newId.Length - 1) : "0";
                var parent = allItems.FirstOrDefault(i => i.Attribute("id")?.Value == parentId);

                if (parent != null)
                {
                    var newItem = new XElement("item",
                        new XAttribute("id", newId),
                        new XAttribute("text", newText),
                        new XAttribute("child", "0"),
                        new XAttribute("im0", "bt.gif"),
                        new XAttribute("im1", "b.gif"),
                        new XAttribute("im2", "bt.gif")
                    );
                    parent.Add(newItem);
                }
                else
                {
                    Console.WriteLine($"⚠️ Kunde inte hitta förälder till {newId}, hoppar över.");
                }
            }

            // 6. Spara resultat
            doc.Save(ResultFile);
            Console.WriteLine($"✅ XML uppdaterad och sparad som '{ResultFile}'.");
        }

        public void updateAndSaveXml_v2(string xmlPath, string csvPath)
        {

            // Läs in XML
            XDocument doc = XDocument.Load(xmlPath);

            // Läs in CSV med semikolon och hämta endast kolumn 0 och 1
            var updates = File.ReadAllLines(csvPath)
                                .Where(line => !string.IsNullOrWhiteSpace(line))
                                .Select(line => line.Split(';'))
                                .Where(parts => parts.Length >= 2)
                                .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());


            // Hämta alla befintliga item-noder
            var allItems = doc.Descendants("item").ToList();
            var itemById = allItems
                .Where(i => i.Attribute("id") != null)
                .ToDictionary(i => i.Attribute("id").Value, i => i);

            //// Regex för ID-validering: minst 3 siffror
            //Regex validIdPattern = new Regex(@"^\d{3,}$");

            // Regex för ID-validering: minst 2 siffror
            Regex validIdPattern = new Regex(@"^\d{2,}$");

            foreach (var kvp in updates)
            {
                string id = kvp.Key;
                string newText = kvp.Value;

                if (!validIdPattern.IsMatch(id))
                {
                    FailedAdditions.Add($"Ogiltigt ID-format: {id} | '{newText}'");
                    continue;
                }

                if (itemById.TryGetValue(id, out XElement existing))
                {
                    string oldText = existing.Attribute("text")?.Value ?? "";
                    existing.SetAttributeValue("text", newText);
                    UpdatedNodes.Add($"Uppdaterade: {id} | '{oldText}' → '{newText}'");
                }
                else
                {
                    string parentId = id.Length > 1 ? id.Substring(0, id.Length - 1) : "0";
                    if (itemById.TryGetValue(parentId, out XElement parent))
                    {
                        var newItem = new XElement("item",
                            new XAttribute("id", id),
                            new XAttribute("text", newText),
                            new XAttribute("child", "0"),
                            new XAttribute("im0", "bt.gif"),
                            new XAttribute("im1", "b.gif"),
                            new XAttribute("im2", "bt.gif")
                        );
                        parent.Add(newItem);
                        AddedNodes.Add($"Tillagd: {id} | '{newText}' under {parentId}");
                    }
                    else
                    {
                        FailedAdditions.Add($"Misslyckad: {id} | '{newText}' – saknar förälder {parentId}");
                    }
                }
            }

            // Sortera alla item-noder inom varje förälder enligt ID
            foreach (var parent in doc.Descendants("item").Where(p => p.Elements("item").Any()))
            {
                var sortedChildren = parent.Elements("item")
                                            .OrderBy(c => c.Attribute("id")?.Value)
                                            .ToList();
                parent.ReplaceNodes(sortedChildren);
            }

            // Spara uppdaterad XML
            doc.Save(ResultFile);
        }
        

        public void CreateChangeReport()
        {
            var reportLines = new List<string>
            {
                "ÄNDRINGSRAPPORT",
                ""
            };

            reportLines.AddRange(UpdatedNodes);
            reportLines.AddRange(AddedNodes);
            reportLines.AddRange(FailedAdditions);

            reportLines.Add("");
            reportLines.Add($"Totalt uppdaterade: {UpdatedNodes.Count}");
            reportLines.Add($"Totalt tillagda: {AddedNodes.Count}");
            reportLines.Add($"Misslyckade tillägg: {FailedAdditions.Count}");

            File.WriteAllLines(ChangeReportFile, reportLines);
            
        }
    }
}
