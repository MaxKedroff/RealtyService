namespace Application.DTOs
{
    public class DetailedFlatDto
    {
        public string Address { get; set; }

        public double Price { get; set; }

        public double Area { get; set; }
        public int Floor { get; set; }
        public int Rooms { get; set; }
        public double KitchenArea { get; set; }
        public string Metro { get; set; }

        public DateTime BuildYear { get; set; }

        public string Material { get; set; }

        public bool hasBalkony { get; set; }

        public DateTime PublicationDate { get; set; }
        public string Source { get; set; }

        public string Finishing { get; set; }
    }
}
