using Elsa.Activities.Console;
using Elsa.Builders;

namespace Elsa.Samples.Server.Host
{
    public class SlowActivityWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            // builder
            //     .WithPersistenceBehavior(Models.WorkflowPersistenceBehavior.WorkflowPassCompleted)
            //     .WriteLine(x => "foo") // dummy activity to coerce persistence
            //     .Then<SlowActivity>(x => x.WithId("number1")) // If the process is stopped during this activity, number1 will be resumed
            //     .Then<SlowActivity>(x => x.WithId("number2"));

            builder
                .WithPersistenceBehavior(Models.WorkflowPersistenceBehavior.WorkflowPassCompleted)
                .StartWith<SlowActivity>(x => x.WithId("number1")) // When the process is killed during this activity no runnable tasks are found
                .Then<SlowActivity>(x => x.WithId("number2"));
        }
    }
}