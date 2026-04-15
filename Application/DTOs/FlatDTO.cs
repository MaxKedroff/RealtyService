using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class FlatDTO
    {
        public Guid Id { get; set; }
        public double area { get; set; }

        public int rooms { get; set; }

        public int floor { get; set; }

        public double Price { get; set; }
        public double SQM { get; set; }
        public string PictureUrl { get; set; }
        public string Source { get; set; }
        public string? coords { get; set; }
    }
}
