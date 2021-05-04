using NodaTime;

namespace Elsa.Client.Models
{
    public class ExecutionLogEntry
    {
        public ExecutionLogEntry()
        {
        }

        public ExecutionLogEntry(string activityId, Instant timestamp)
        {
            ActivityId = activityId;
            Timestamp = timestamp;
        }

        public string ActivityId { get; set; } = default!;
        public Instant Timestamp { get; set; }
    }
}