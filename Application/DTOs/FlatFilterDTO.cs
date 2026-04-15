using Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class FlatFilterDTO
    {
        public Guid CityId { get; set; }
        public double minArea { get; set; }
        public double maxArea { get; set; }
        public int minRooms { get; set; }
        public int maxRooms { get; set; }
        public int minFloor { get; set; }
        public int maxFloor { get; set; }
        public double minPrice { get; set; }
        public double maxPrice { get; set; }
        public double minSQM { get; set; }
        public double maxSQM { get; set; }
        public string? GeoPoint { get; set; }
        public int Limit { get; set; } = 100;

    }
}
