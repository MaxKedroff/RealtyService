using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class SimilarFlat
    {
        public Guid FlatId { get; set; }
        public double Price { get; set; }
        public double Area { get; set; }
        public int Rooms { get; set; }
        public int Floor { get; set; }
        public double SimilarityScore { get; set; }
        public string Address { get; set; } = "";
        public double PricePerSqm { get; set; }
        public DateTime? BuildingYear { get; set; }
        public double? DistanceToMetro { get; set; }
    }
}
