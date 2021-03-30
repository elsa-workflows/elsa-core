using Elsa.Activities.Console;
using Elsa.Activities.Temporal;
using Elsa.Builders;
using NodaTime;

namespace Elsa.Samples.Server.Host.Workflows
{
    public class HeartbeatWorkflow : IWorkflow
    {
        private readonly IClock _clock;
        public HeartbeatWorkflow(IClock clock) => _clock = clock;

        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WithDisplayName("Timer")
                .Timer(Duration.FromSeconds(10))
                .WriteLine(() => $"Heartbeat at {_clock.GetCurrentInstant()}");
        }
    }
}