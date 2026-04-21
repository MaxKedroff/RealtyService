using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class PredictionInput
    {
        [JsonPropertyName("Area")]
        public double Area { get; set; }

        [JsonPropertyName("Rooms")]
        public int Rooms { get; set; }

        [JsonPropertyName("Floor")]
        public int Floor { get; set; }

        [JsonPropertyName("BuildYear")]
        public DateTime? BuildYear { get; set; }

        [JsonPropertyName("PropertyType")]
        public string PropertyType { get; set; } = "flat";

        [JsonPropertyName("HouseType")]
        public string HouseType { get; set; } = "secondary";

        [JsonPropertyName("District")]
        public string District { get; set; } = "unknown";

        [JsonPropertyName("DealType")]
        public string DealType { get; set; } = "sale";

        [JsonPropertyName("Description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("Address")]
        public string Address { get; set; } = "";
    }
}
