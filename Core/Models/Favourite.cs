using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;


namespace Core.Models
{
    public class Favourite
    {

        [Description("Идентификатор избранного")]
        public Guid FavouriteId { get; set; }


        [Description("Избранная конфигурация параметров")]
        [Column("PARAMETERS")]
        public string? ParametersJson { get; set; } 

        [NotMapped]
        [JsonProperty("PARAMETERS")]
        public JObject? Parameters
        {
            get => string.IsNullOrEmpty(ParametersJson) ? null : JObject.Parse(ParametersJson);
            set => ParametersJson = value?.ToString(Formatting.None);
        }


        [Description("Цвет полигона")]
        [JsonProperty("COLOR")]
        public string? Color { get; set; }


        [Description("Координаты границ полигонов")]
        [JsonProperty("GEO_POINTS")]
        public string? GeoPoints { get; set; }

        [Description("Дата сохранения")]
        [JsonProperty("CREATE_DATE")]
        public DateTime CreateDate { get; set; }
    }
}
