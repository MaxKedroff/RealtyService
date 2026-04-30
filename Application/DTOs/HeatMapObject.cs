using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class HeatMapObject
    {
        public string coordinates { get; set; }
        public int FlatsCount { get; set; }
        public decimal MedianActualPrice { get; set; }

        public DateTime YearBuilt { get; set; }

        public decimal? MedianPredictedPrice { get; set; }

    }
}
