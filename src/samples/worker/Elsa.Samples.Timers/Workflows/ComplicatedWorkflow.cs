using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Timers;
using Elsa.Builders;
using NodaTime;

namespace Elsa.Samples.Timers.Workflows
{
    public class ComplicatedWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .AsSingleton()
                .For(0, 3, iterate => iterate
                    .Then<Fork>(fork => fork.WithBranches("A", "B"), fork =>
                    {
                        fork
                            .When("A")
                            .Timer(Duration.FromSeconds(10)).WithId("timer-a")
                            .WriteLine("Timer A went off. Exiting fork").WithId("write-line-a")
                            .Then("Join2");

                        fork
                            .When("B")
                            .While(true, iterate => iterate
                                .IfTrue(true, ifTrue => ifTrue
                                    .Timer(Duration.FromSeconds(1)).WithId("timer-b")
                                    .WriteLine("Timer B went off. Looping.").WithId("write-line-b"))
                            ).WithId("while-true")
                            .Then("Join2");
                    })
                    .Add<Join>(join => join.WithMode(Join.JoinMode.WaitAny)).WithName("Join2")
                ).WithId("for-1")
                .WriteLine("Workflow done.");
        }
    }
}