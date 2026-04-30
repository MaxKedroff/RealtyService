using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class PolygonDto
    {
        public Guid id { get; set; }
        public string color { get; set; }
        public string GeoPoints { get; set; }

        public string? Parameters { get; set; }
    }
}
