using Elsa.Activities.Console;
using Elsa.Builders;
using Elsa.Samples.Timers.Activities;
using NodaTime;

namespace Elsa.Samples.Timers.Workflows
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
                .WriteLine("Started")
                .Then<MyContainer2>()
                .WriteLine("Finished");
        }
    }
}