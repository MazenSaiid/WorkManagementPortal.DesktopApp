using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkTrackerWPFApp.Dtos
{
    public class ActivityLog
    {
        public string ApplicationName { get; set; }
        public string Website { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double DurationInSeconds { get; set; }
    }
}
