namespace WorkTrackerDesktop.Responses
{
    public class PauseTrackingResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public PauseTrackingLog PauseTrackingLog { get; set; }
    }
}
