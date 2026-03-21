using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingService.Models
{
    public class ParsingOptions
    {
        public string Query { get; set; }
        public int MaxResults { get; set; } = 50;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
}
