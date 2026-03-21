using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingService.Models
{
    public class ParsingResult
    {
        public string Source { get; set; }
        public DateTime ParsedAt { get; set; }
        public List<PropertyItem> Properties { get; set; } = new();

        public string DirtyJson { get; set; } = "";

        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
