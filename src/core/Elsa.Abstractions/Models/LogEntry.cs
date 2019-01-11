using NodaTime;

namespace Elsa.Models
{
    public class LogEntry
    {
        public LogEntry()
        {
        }

        public LogEntry(string activityId, Instant timestamp, string message, bool faulted = false)
        {
            ActivityId = activityId;
            Timestamp = timestamp;
            Message = message;
            Faulted = faulted;
        }

        public string ActivityId { get; set; }
        public Instant Timestamp { get; set; }
        public bool Faulted { get; set; }
        public string Message { get; set; }
    }
}