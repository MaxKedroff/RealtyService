using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class Metro
    {
        [Column("ID")]
        public Guid MetroId { get; set; }
        [Description("Идентификатор города")]
        [Column("CITY_ID")]
        public Guid CityId { get; set; }

        [Description("Город")]
        public required City City { get; set; }

        [Description("Название станции")]
        [Column("STATION_NAME")]
        public required string StationName { get; set; }

        [Description("Координаты станции")]
        [Column("GEO_POINT")]
        public required string GeoPoint { get; set; }
    }
}
