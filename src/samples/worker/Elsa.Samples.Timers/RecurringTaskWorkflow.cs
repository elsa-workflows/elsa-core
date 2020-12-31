using System;
using System.Threading.Tasks;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
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
                .WriteLine("Started")
                .Then<Fork>(fork => fork.WithBranches("A", "B"), fork =>
                {
                    fork
                        .When("A")
                        .Then<MyContainer1>()
                        .Then("Join3");

                    fork
                        .When("B")
                        .Then<MyContainer2>()
                        .Then("Join3");
                })
                .Add<Join>(join => join.WithMode(Join.JoinMode.WaitAny)).WithName("Join3")
                .WriteLine("Workflow Joined!")
                .WriteLine("Finished");
        }
    }
}