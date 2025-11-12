using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

public class XmlUpdater
{
    public void ApplyUpdates(XDocument doc, Dictionary<string, string> updates)
    {
        var allItems = doc.Descendants("item").ToList();

        foreach (var item in allItems)
        {
            string id = item.Attribute("id")?.Value;
            if (id != null && updates.TryGetValue(id, out string newText))
            {
                item.SetAttributeValue("text", newText);
            }
        }
    }

    public void AddMissingItems(XDocument doc, Dictionary<string, string> updates)
    {
        var allItems = doc.Descendants("item").ToList();
        var existingIds = new HashSet<string>(allItems.Select(i => i.Attribute("id")?.Value));

        foreach (var kvp in updates)
        {
            string newId = kvp.Key;
            string newText = kvp.Value;

            if (existingIds.Contains(newId)) continue;

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
        }
    }

    public Dictionary<string, string> ParseCsv(string[] lines)
    {
        return lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split(';'))
            .Where(parts => parts.Length >= 2)
            .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());
    }
}
