using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class SavedPolygonsDto
    {
        public int Amount { get; set; }
        public List<PolygonDto> SavedPolygons { get; set; }
    }
}
