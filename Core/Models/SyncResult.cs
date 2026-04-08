using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class SyncResult
    {
        public string Source { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int NewFlats { get; set; }
        public int UpdatedFlats { get; set; }
        public int NewBuildings { get; set; }
        public int UpdatedBuildings { get; set; }
        public int DeactivatedFlats { get; set; }
        public int SkippedFlats { get; set; }
        public int TotalProcessed { get; set; }
        public List<string> Errors { get; set; }
        public Dictionary<string, object> Metadata { get; set; }

        public SyncResult()
        {
            Errors = new List<string>();
            Metadata = new Dictionary<string, object>();
        }
    }
}
