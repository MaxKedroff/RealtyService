using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class FlatAnalogDto
    {
        [JsonPropertyName("flat_id")]
        public Guid FlatId { get; set; }

        public double Price { get; set; }

        public double Similarity { get; set; }

        public double Area { get; set; }

        public int Rooms { get; set; }

        public int Floor { get; set; }
    }
}
