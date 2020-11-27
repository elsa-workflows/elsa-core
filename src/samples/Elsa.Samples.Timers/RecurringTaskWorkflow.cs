using Elsa.Activities.Console;
using Elsa.Activities.Timers;
using Elsa.Builders;
using Elsa.Models;
using NodaTime;

namespace Elsa.Samples.Timers
{
    public class RecurringTaskWorkflow : IWorkflow
    {
        private readonly IClock _clock;

        public RecurringTaskWorkflow(IClock clock)
        {
            _clock = clock;
        }
        
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .AsSingleton()
                .TimerEvent(Duration.FromSeconds(2))
                .WriteLine(context => $"{context.WorkflowExecutionContext.WorkflowInstance.WorkflowInstanceId} triggered by timer at {_clock.GetCurrentInstant()}.")
                .TimerEvent(Duration.FromSeconds(2))
                .WriteLine(context => $"{context.WorkflowExecutionContext.WorkflowInstance.WorkflowInstanceId} resumed by timer at {_clock.GetCurrentInstant()}.");
        }
    }
}