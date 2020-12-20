using Elsa.Activities.Console;
using Elsa.Activities.Timers;
using Elsa.Builders;
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
                .Timer(Duration.FromSeconds(5))
                .WriteLine(context => $"{context.WorkflowExecutionContext.WorkflowInstance.Id} triggered by timer at {_clock.GetCurrentInstant()}.")
                .StartIn(Duration.FromSeconds(5))
                .WriteLine(context => $"{context.WorkflowExecutionContext.WorkflowInstance.Id} resumed by timer at {_clock.GetCurrentInstant()}.");
        }
    }
}