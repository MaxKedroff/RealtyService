using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class PredictByParametersResult
    {
        public DateTime PredictionTime { get; set; }
        public double PredictedPrice { get; set; }
        public double PredictedPriceMln { get; set; }
        public string Currency { get; set; }
        public string ModelVersion { get; set; }
        public string ModelName { get; set; }
    }

    public class PredictByParametersRequest
    {
        public double FlatArea { get; set; }
        public int FlatRooms { get; set; }
        public int FlatFloor { get; set; }
        public double? FlatAreaKitchen { get; set; }
        public double? FlatAreaLiving { get; set; }
        public int? FlatBalcony { get; set; }
        public int? FlatLoggia { get; set; }
        public int? FlatFurniture { get; set; }
        public string FlatStatus { get; set; }
        public Guid CityId { get; set; }
        public int? TotalFloors { get; set; }
        public int? BuildYear { get; set; }
        public string Renovation { get; set; }
        public string Source { get; set; }
    }
}
