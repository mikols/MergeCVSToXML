using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace WpfSNIOcdesToXML
{
    public class XMLData
    {
        private string xmlFile = "";
        private string csvFile = "";
        private string csvFile2 = "";

        public string ResultFileXML = string.Empty;
        public string ChangeReportFile = string.Empty;


        public string XmlFIle { get => xmlFile; set => xmlFile = value; }

        public string CsvFIle { get => csvFile; set => csvFile = value; }

        public string CsvFile2 { get => csvFile2; set => csvFile2 = value; }


        public List<string> UpdatedNodes = new List<string>();
        public List<string> AddedNodes = new List<string>();
        public List<string> FailedAdditions = new List<string>();


        public bool RUN()
        {
            ResultFileXML = string.Empty;
            if (File.Exists(xmlFile) && File.Exists(csvFile) && File.Exists(csvFile2))
            {
                // Hämta katalogen där XML-filen ligger
                string xmlDirectory = Path.GetDirectoryName(Path.GetFullPath(xmlFile));

                //ResultFile = Path.Combine(xmlDirectory, $"updated.XML.{DateTime.Now:yyyyMMdd_HHmm}.xml");
                ResultFileXML = Path.Combine(xmlDirectory, $"updated.XML.xml");
                //ChangeReportFile = Path.Combine(xmlDirectory, $"changeReport.{DateTime.Now:yyyyMMdd_HHmm}.txt");
                ChangeReportFile = Path.Combine(xmlDirectory, $"changeReport.txt");

                UpdateXmlWithCsv_v3(xmlFile, csvFile2);
                // CreateChangeReport_v2();
                return true;
            }
            return false;
        }


        #region Version 2

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
            doc.Save(ResultFileXML);
        }
        
        public void CreateChangeReport_v2()
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

        #endregion

        private string NormalizeId(string id)
        {
            return id?.Replace(".", "");
        }


        private void UpdateXmlWithCsv_v3(string xmlPath, string csvPath)
        {
            XDocument doc = XDocument.Load(xmlPath);
            var logEntries = new List<ChangeLogEntry>();

            var csvLines = File.ReadAllLines(csvPath)
                               .Skip(1)
                               .Select(line => line.Split(';'))
                               .Where(parts => parts.Length >= 7)
                               .Select(parts => new
                               {
                                   Huvudgrupp = parts[2],
                                   Grupp = parts[3],
                                   Undergrupp = parts[4],
                                   Detaljgrupp = parts[5],
                                   Text = parts[6]
                               });

            foreach (var row in csvLines)
            {
                string id = null;
                if (!string.IsNullOrWhiteSpace(row.Huvudgrupp)) id = row.Huvudgrupp;
                if (!string.IsNullOrWhiteSpace(row.Grupp)) id = row.Huvudgrupp + row.Grupp;
                if (!string.IsNullOrWhiteSpace(row.Undergrupp)) id = row.Huvudgrupp + row.Undergrupp;
                if (!string.IsNullOrWhiteSpace(row.Detaljgrupp)) id = row.Detaljgrupp;
                id = NormalizeId(id); // <-- här tas punkter bort


                if (string.IsNullOrEmpty(id)) continue;

                var existingItem = doc.Descendants("item")
                                      .FirstOrDefault(x => NormalizeId((string)x.Attribute("id")) == id);


                if (existingItem != null)
                {
                    string oldText = (string)existingItem.Attribute("text");
                    existingItem.SetAttributeValue("text", row.Text);

                    logEntries.Add(new ChangeLogEntry
                    {
                        Id = id,
                        Action = "Updated",
                        OldText = oldText,
                        NewText = row.Text
                    });
                }
                else
                {
                    XElement newItem = new XElement("item",
                        new XAttribute("id", id),
                        new XAttribute("text", row.Text),
                        new XAttribute("child", "0"),
                        new XAttribute("im0", "bt.gif"),
                        new XAttribute("im1", "b.gif"),
                        new XAttribute("im2", "bt.gif"));

                    string parentId = id.Length > 2 ? id.Substring(0, id.Length - 1) : null;
                    var parent = parentId == null ? doc.Root :
                                 doc.Descendants("item").FirstOrDefault(x => (string)x.Attribute("id") == parentId);

                    if (parent != null)
                    {
                        parent.Add(newItem);
                        logEntries.Add(new ChangeLogEntry
                        {
                            Id = id,
                            Action = "Added",
                            OldText = "",
                            NewText = row.Text
                        });
                    }
                    else
                    {
                        logEntries.Add(new ChangeLogEntry
                        {
                            Id = id,
                            Action = "Error",
                            OldText = "",
                            NewText = row.Text
                        });
                    }
                }
            }

            // Spara ny XML
            string newFile = Path.Combine(Path.GetDirectoryName(xmlPath),
                $"updated_{DateTime.Now:yyyyMMdd}.xml");
            doc.Save(newFile);

            //// Spara rapport som textfil
            //string reportFile = Path.Combine(Path.GetDirectoryName(xmlPath),
            //    $"report_{DateTime.Now:yyyyMMdd_HHmm}.txt");

            using (var writer = new StreamWriter(ChangeReportFile))
            {
                writer.WriteLine("=== Rapport över ändringar ===");
                writer.WriteLine($"Datum: {DateTime.Now}");
                writer.WriteLine($"Ny XML-fil: {newFile}");
                writer.WriteLine();

                foreach (var entry in logEntries)
                {
                    if (entry.Action == "Updated")
                        writer.WriteLine($"[UPDATED] ID {entry.Id}: \"{entry.OldText}\" → \"{entry.NewText}\"");
                    else if (entry.Action == "Added")
                        writer.WriteLine($"[ADDED]   ID {entry.Id}: \"{entry.NewText}\"");
                    else if (entry.Action == "Error")
                        writer.WriteLine($"[ERROR]   ID {entry.Id}: Kunde inte placeras. Text: \"{entry.NewText}\"");
                }
            }

            MessageBox.Show($"Ny XML sparad som: {newFile}\nRapport sparad som: {ChangeReportFile}");
        }

    }
}
