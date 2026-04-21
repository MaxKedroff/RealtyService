using Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class PredictionResult
    {
        public double PredictedPrice { get; set; }
        public double PredictedPriceMln { get; set; }
        public double Confidence { get; set; }
        public string ModelVersion { get; set; } = "";
        public string Status { get; set; } = "";
        public List<SimilarFlat> SimilarFlats { get; set; } = new();
        public Dictionary<string, double> FeatureImportance { get; set; } = new();
        public DateTime PredictionTime { get; set; }
        public double ActualPrice { get; set; }
        public double DeviationPercent { get; set; }
        public string Recommendation { get; set; } = "";
    }
}
