using Elsa.Activities.Console;
using Elsa.Activities.Temporal;
using Elsa.Builders;
using NodaTime;

namespace Elsa.Samples.Timers.Workflows
{
    /// <summary>
    /// A workflow that executes only once in the near future.
    /// </summary>
    public class OneOffWorkflow : IWorkflow
    {
        private readonly Instant _executeAt;
        private readonly IClock _clock;

        public OneOffWorkflow(Instant executeAt, IClock clock)
        {
            _executeAt = executeAt;
            _clock = clock;
        }
        
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartAt(_executeAt)
                .WriteLine(() => $"Started at {_clock.GetCurrentInstant()}. Next event happens 3 seconds from now.")
                .StartIn(Duration.FromSeconds(3))
                .WriteLine(() => $"Follow-up occurred at {_clock.GetCurrentInstant()}.");
        }
    }
}