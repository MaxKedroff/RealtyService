using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class HeatMapDto
    {
        public int Amount { get; set; }
        public List<HeatMapObject> HeatMapObjects { get; set; }
    }
}
