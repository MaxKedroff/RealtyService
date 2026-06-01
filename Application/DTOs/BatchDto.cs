using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class BatchPredictionRequest
    {
        public List<Guid> FlatIds { get; set; }
    }

    public class BatchPredictionResult
    {
        public int TotalProcessed { get; set; }
        public int Successful { get; set; }
        public int Failed { get; set; }
        public List<PredictionResult> Results { get; set; }
        public DateTime ProcessingTime { get; set; }
    }

    public class BatchAnalogsRequest
    {
        public List<Guid> FlatIds { get; set; }
        public int? TopCount { get; set; } 
    }

    public class FlatAnalogsResult
    {
        public Guid FlatId { get; set; }
        public List<FlatAnalogDto> Analogs { get; set; }
        public string Error { get; set; }
    }

    public class BatchAnalogsResult
    {
        public int TotalProcessed { get; set; }
        public int Successful { get; set; }
        public int Failed { get; set; }
        public List<FlatAnalogsResult> Results { get; set; }
        public DateTime ProcessingTime { get; set; }
    }
}
