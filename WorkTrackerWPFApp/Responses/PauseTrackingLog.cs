using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkTrackerDesktopWPFApp.Responses
{
    public class PauseTrackingLog
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTime WorkDate { get; set; } // Change DateOnly to DateTime
        public int PauseType { get; set; }
        public int WorkTrackingLogId { get; set; }
        public DateTime PauseStart { get; set; }
        public DateTime? PauseEnd { get; set; } // Nullable DateTime
        public double PauseDurationInMinutes { get; set; }
        public bool PauseIsFinished { get; set; }
    }
}
