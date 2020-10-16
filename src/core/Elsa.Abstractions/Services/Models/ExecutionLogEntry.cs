using NodaTime;

namespace Elsa.Services.Models
{
    public class ExecutionLogEntry : IExecutionLogEntry
    {
        public ExecutionLogEntry(string activityId, Instant timestamp)
        {
            ActivityId = activityId;
            Timestamp = timestamp;
        }

        public string ActivityId { get; }
        public Instant Timestamp { get; }
    }
}