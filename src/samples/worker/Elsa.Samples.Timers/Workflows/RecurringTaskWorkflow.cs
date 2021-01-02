using Elsa.Activities.Console;
using Elsa.Builders;
using Elsa.Samples.Timers.Activities;

namespace Elsa.Samples.Timers.Workflows
{
    public class RecurringTaskWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .WriteLine("Started")
                .Then<MyContainer2>()
                .WriteLine("Finished");
        }
    }
}