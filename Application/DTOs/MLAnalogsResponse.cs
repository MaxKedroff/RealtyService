namespace Application.DTOs
{
    public class MLAnalogsResponse
    {
        public double PredictedPrice { get; set; }

        public List<FlatAnalogDto> Analogs { get; set; } = new();

        public int TotalFound { get; set; }
    }
}
