using Elsa.Activities.Console;
using Elsa.Activities.Timers;
using Elsa.Builders;
using Elsa.Models;
using NodaTime;

namespace Elsa.Samples.MultiTenantChildWorker.Workflows
{
    public class DebugStartAtWorkflow : IWorkflow
    {
        private readonly IClock _clock;

        public DebugStartAtWorkflow(IClock clock)
        {
            _clock = clock;
        }
        
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .StartAt(_clock.GetCurrentInstant().Plus(Duration.FromSeconds(5)))
                .WriteLine(() => $"Start at: {_clock.GetCurrentInstant()}: Tick 1")
                .StartAt(_clock.GetCurrentInstant().Plus(Duration.FromSeconds(2)))
                .WriteLine(() => $"Start at: {_clock.GetCurrentInstant()}: Tick 2");
        }
    }
}