namespace WorkTrackerDesktopWPFApp.Services
{
    public class WorkSessionService
    {
        private static WorkSessionService _instance;

        public int? WorkLogId { get; set; }
        public int PauseId { get; set; }
        public string UserId { get; set; }
        public DateTime WorkTimeStart { get; set; }
        public DateTime WorkTimeEnd { get; set; }
        public static WorkSessionService Instance => _instance ??= new WorkSessionService();

        public void ClearWorkSession()
        {
            WorkLogId = 0;
            PauseId = 0;
            WorkTimeStart = default;
            WorkTimeEnd = default;
            UserId = null;
        }
    }
}
