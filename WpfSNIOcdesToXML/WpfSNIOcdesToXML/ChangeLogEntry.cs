using System;
using System.Collections.Generic;
using System.Linq;


namespace WpfSNIOcdesToXML
{
    public class ChangeLogEntry
    {
        public string Id { get; set; }
        public string Action { get; set; } // "Added", "Updated", "Error"
        public string OldText { get; set; }
        public string NewText { get; set; }
    }

}
