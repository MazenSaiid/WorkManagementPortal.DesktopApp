namespace WorkTrackerDesktop.Responses
{
    public class WorkTrackingLog
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTime WorkDate { get; set; }  // Change DateOnly to DateTime
        public DateTime WorkTimeStart { get; set; }
        public DateTime WorkTimeEnd { get; set; }
        public bool HasFinished { get; set; }
        public bool IsWorking { get; set; }
        public bool IsPaused { get; set; }
        public List<PauseTrackingLog> PauseTrackingLogs { get; set; }  // Flattened list of PauseTrackingLogs
        public double ActualWorkDurationInHours { get; set; }
    }
}
