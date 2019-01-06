using System.Collections.Generic;
using NodaTime;

namespace Elsa.Models
{
    public class LogEntry
    {
        public string ActivityId { get; set; }
        public Instant? ExecutedAt { get; set; }
        public Instant? HaltedAt { get; set; }
        public Instant? ResumedAt { get; set; }
        public Instant? CompletedAt { get; set; }
        public IList<string> TriggeredEndpoints { get; set; }
        public ActivityFault Fault { get; set; }
    }
}