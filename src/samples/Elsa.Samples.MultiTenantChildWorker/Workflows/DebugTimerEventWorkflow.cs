using Elsa.Activities.Console;
using Elsa.Activities.Timers;
using Elsa.Builders;
using NodaTime;

namespace Elsa.Samples.MultiTenantChildWorker.Workflows
{
    public class DebugTimerEventWorkflow : IWorkflow
    {
        private readonly IClock _clock;

        public DebugTimerEventWorkflow(IClock clock)
        {
            _clock = clock;
        }
        
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .AsSingleton()
                .TimerEvent(Duration.FromSeconds(3))
                .WriteLine(() => $"Timer: {_clock.GetCurrentInstant()}: Tick 1")
                .TimerEvent(Duration.FromSeconds(3))
                .WriteLine(() => $"Timer: {_clock.GetCurrentInstant()}: Tick 2");
        }
    }
}