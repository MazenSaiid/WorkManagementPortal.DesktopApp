using WorkTrackerWPFApp.Responses;

namespace WorkTrackerDesktopWPFApp.Responses
{
    public class WorkTrackingResponse : ValidationResponse
    {
        public WorkTrackingLog WorkTrackingLog { get; set; }  // Changed to WorkTrackingLog for nested object
    }
}
