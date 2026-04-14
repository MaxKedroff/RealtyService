using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class GetFlatFromBuildingsResultDTO
    {
        public IEnumerable<FlatDTO> Flats { get; set; }
        public int FlatCount { get; set; }
        public string address { get; set; }
    }
}
