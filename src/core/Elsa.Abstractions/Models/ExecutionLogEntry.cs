using NodaTime;

namespace Elsa.Models
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

        public string ActivityId { get; set; }
        public Instant Timestamp { get; set; }
        public bool Faulted { get; set; }
        public string Message { get; set; }
    }
}