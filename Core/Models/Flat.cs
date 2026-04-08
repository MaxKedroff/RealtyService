using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;


namespace Core.Models
{
    public class Flat
    {
        [Description("идентификатор квартиры")]
        [Column("FLAT_ID")]
        public Guid FlatId { get; set; }

        public string ExternalId { get; set; }

        [Description("Площадь квартиры")]
        [Column("FLAT_AREA")]
        public double FlatArea { get; set; }

        [Description("Площадь жилая")]
        [Column("FLAT_AREA_LIVING")]
        public double FlatAreaLiving { get; set; }

        [Description("Площадь кухни")]
        [Column("FLAT_AREA_KITCHEN")]
        public double FlatAreaKitchen { get; set; }

        [Description("Количество комнат")]
        [Column("FLAT_ROOMS")]
        public int FlatRooms { get; set; }

        [Description("Этаж квартиры")]
        [Column("FLAT_FLOOR")]
        public int FlatFloor { get; set; }

        [Description("Флаг: есть ли балкон")]
        [Column("FLAT_BALCONY")]
        public bool FlatBalcony { get; set; }

        [Description("Флаг: есть ли лоджия")]
        [Column("FLAT_LOGGIA")]
        public bool FlatLoggia { get; set; }

        [Description("Тип ремонта в квартире")]
        [Column("TYPES_RENOVATION")]
        public TypesRenovation Renovation { get; set; }

        [Description("Стоимость квартиры, млн")]
        [Column("FLAT_PRICE")]
        public double FlatPrice { get; set; }

        [Description("Стоимость квартиры за кв м в тыс")]
        [Column("FLAT_PRICE_SQM")]
        public double FlatPriceSQM { get; set; }

        [Description("Статус квартиры")]
        [Column("FLAT_STATUS")]
        public Status FlatStatus { get; set; }

        [Description("Дата публикации")]
        [Column("FLAT_PUBLISHED")]
        public DateTime FlatPublished { get; set; }

        [Description("Дата снятия с публикации")]
        [Column("FLAT_UNPUBLISHED")]
        public DateTime? FlatUnpublished { get; set; }

        [Description("Есть ли мебель")]
        [Column("FLAT_FURNITURE")]
        public bool FlatFurniture { get; set; }

        [Description("Ссылка на фотографию")]
        [Column("PICTURE_ID")]
        public string PictureUrl { get; set; }

        public string Source { get; set; }

        public bool IsActive { get; set; }


        [Description("Идентификатор дома")]
        [Column("BUILDING_ID")]
        public Guid BuildingId { get; set; }

        public required Building Building { get; set; }

        [Description("Идентификатор города")]
        [Column("CITY_ID")]
        public Guid CityId { get; set; }

        public required City City { get; set; }
    }

}
