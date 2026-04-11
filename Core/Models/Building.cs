using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class Building
    {
        [Description("Идентификатор дома")]
        [Column("BUILDING_ID")]
        public Guid BuildingId { get; set; }

        public string ExternalId { get; set; }

        [Description("Адрес дома")]
        [Column("ADDRESS")]
        public required string Address { get; set; }

        [Description("Координаты дома")]
        [Column("GEO_POINT")]
        public required string GeoPoint { get; set; }

        [Description("Год постройки дома")]
        [Column("YEAR_BUILDT")]
        public DateTime YearBuild { get; set; }

        [Description("Материал стен дома")]
        [Column("WALL_MATERIAL")]
        public WallMaterial WallMaterial { get; set; }

        [Description("Количество этажей в доме")]
        [Column("TOTAL_FLOORS")]
        public int TotalFloors { get; set; }

        [Description("Флаг: новостройка ли")]
        [Column("IS_NEW")]
        public bool IsNew { get; set; }

        [Description("Дистанция до метро в км")]
        [Column("METRO_DISTANCE")]
        public double? MetroDistance { get; set; }

        public ICollection<Flat> Flats { get; set; } = new List<Flat>();
    }
}
