using Elsa.Activities.Console;
using Elsa.Activities.Timers;
using Elsa.Builders;
using Elsa.Models;
using NodaTime;

namespace Elsa.Samples.MultiTenantChildWorker.Workflows
{
    public class DebugCronEventWorkflow : IWorkflow
    {
        private readonly IClock _clock;

        public DebugCronEventWorkflow(IClock clock)
        {
            _clock = clock;
        }
        
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .AsSingleton()
                .WithDeleteCompletedInstances(true)
                .WithPersistenceBehavior(WorkflowPersistenceBehavior.Suspended)
                .CronEvent("0/3 * * ? * * *")
                .WriteLine(() => $"Cron: {_clock.GetCurrentInstant()}: Tick 1")
                .CronEvent("0/10 * * ? * * *")
                .WriteLine(() => $"Cron: {_clock.GetCurrentInstant()}: Tick 2");
        }
    }
}