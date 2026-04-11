using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class BuildingDTO
    {
        public Guid BuildingId { get; set; }
        public string Address { get; set; }
        public int FlatsCount { get; set; }
        public DateTime YearBuild { get; set; }
        public string GeoPoint { get; set; }
    }
}
