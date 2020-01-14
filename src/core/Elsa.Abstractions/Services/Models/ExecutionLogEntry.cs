using NodaTime;

namespace Elsa.Services.Models
{
    public class ExecutionLogEntry
    {
        public ExecutionLogEntry(IActivity activity, Instant timestamp, string message)
        {
            Activity = activity;
            Timestamp = timestamp;
        }

        public IActivity Activity { get; }
        public Instant Timestamp { get; }
    }
}