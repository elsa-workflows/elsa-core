using Elsa.Activities.Console;
using Elsa.Activities.Temporal;
using Elsa.Builders;
using NodaTime;

namespace ElsaDashboard.Samples.AspNetCore.Monolith.Workflows
{
    public class HeartbeatWorkflow : IWorkflow
    {
        private readonly IClock _clock;
        public HeartbeatWorkflow(IClock clock) => _clock = clock;

        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WithDisplayName("Timer")
                .Timer(Duration.FromSeconds(30))
                .WriteLine(() => $"Heartbeat at {_clock.GetCurrentInstant()}");
        }
    }
}