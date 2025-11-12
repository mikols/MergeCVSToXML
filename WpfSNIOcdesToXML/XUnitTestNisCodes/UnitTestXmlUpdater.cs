using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace XUnitTestNisCodes
{
    public class UnitTestXmlUpdater
    {
        [Fact]
        public void Should_Update_Existing_Item_Text()
        {
            var xml = @"<tree><item id='123' text='Old Text'/></tree>";
            var doc = XDocument.Parse(xml);
            var updates = new Dictionary<string, string> { { "123", "New Text" } };

            var updater = new XmlUpdater(); // din klass
            updater.ApplyUpdates(doc, updates);

            var updatedText = doc.Descendants("item").First().Attribute("text").Value;
            Assert.Equal("New Text", updatedText);
        }

    }
}
