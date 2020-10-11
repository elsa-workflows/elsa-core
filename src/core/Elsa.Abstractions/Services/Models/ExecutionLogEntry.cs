using NodaTime;

namespace Elsa.Services.Models
{
    public class ExecutionLogEntry : IExecutionLogEntry
    {
        public ExecutionLogEntry(IActivity activity, Instant timestamp)
        {
            Activity = activity;
            Timestamp = timestamp;
        }

        public IActivity Activity { get; }
        public Instant Timestamp { get; }
    }
}