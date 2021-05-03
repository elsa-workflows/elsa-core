using System.Collections.Generic;
using NodaTime;

namespace Elsa.Server.Api.Endpoints.ActivityStats
{
    public record ActivityStats
    {
        public IList<ActivityEventCount> EventCounts { get; set; } = default!;
        public ActivityFault? Fault { get; set; }
        public Duration? AverageExecutionTime { get; set; }
        public Duration? FastestExecutionTime { get; set; }
        public Duration? SlowestExecutionTime { get; set; }
        public Instant? LastExecutedAt { get; set; }
    }

    public record ActivityFault(string Message);
    public record ActivityEventCount(string EventName, int Count);
    internal record ActivityExecutionStat(Instant Timestamp, Duration Duration);
}