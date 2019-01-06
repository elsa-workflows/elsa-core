namespace Elsa.Models
{
    public class ActivityExecutionContext
    {
        public ActivityExecutionContext(IActivity activity)
        {
            Activity = activity;
            LogEntry = new LogEntry
            {
                ActivityId = activity.Id
            };
        }

        public IActivity Activity { get; }
        public LogEntry LogEntry { get; set; }
    }
}
