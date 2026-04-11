using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class GetBuildingsResultDTO
    {
        public IEnumerable<BuildingDTO> Buildings { get; set; }
        public int Amount { get; set; }
    }
}
