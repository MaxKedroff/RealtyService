using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsingService.Models
{
    public class PropertyItem
    {
        public string Id { get; set; }
        public string DealType { get; set; }
        public string PropertyType { get; set; }
        public string Status { get; set; }
        public decimal Price { get; set; }
        public double PPM { get; set; }
        public double Area { get; set; }
        public int? Rooms { get; set; }
        public int? Floor { get; set; }
        public bool? isApart { get; set; }
        public string zkName { get; set; }
        public string? EndOfBuilding { get; set; }
        public string Address { get; set; }
        public string coords { get; set; }
        public string planUrl { get; set; }
        public string Url { get; set; }
        public string? Description { get; set; }
    }
}
