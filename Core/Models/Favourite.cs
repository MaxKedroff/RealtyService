using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;


namespace Core.Models
{
    public class Favourite
    {

        [Description("Идентификатор избранного")]
        public Guid FavouriteId { get; set; }


        [Description("Избранная конфигурация параметров")]
        [JsonProperty("PARAMETERS")]
        public JObject? Parameters { get; set; }


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
