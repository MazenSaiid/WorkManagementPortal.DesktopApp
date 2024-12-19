namespace WorkTrackerDesktopWPFApp.Responses
{
    public class WorkTrackingResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public WorkTrackingLog WorkTrackingLog { get; set; }  // Changed to WorkTrackingLog for nested object
    }
}
