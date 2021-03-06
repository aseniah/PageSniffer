
using System;
using System.Collections.Generic;

namespace PageSniffer.Models
{
    class WebPage
    {
        public bool Enabled { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string NodePath { get; set; }
        public string NodeFilter { get; set; }
        public string AlertTrigger { get; set; }
        public bool AlertActive { get; set; }
        public DateTime NextRun { get; set; }
        public List<string> KnownResults { get; set; }

        public WebPage()
        {
            KnownResults = new List<string>();
        }
    }
}