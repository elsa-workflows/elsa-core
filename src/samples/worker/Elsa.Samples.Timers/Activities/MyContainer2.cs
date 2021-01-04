using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Timers;
using Elsa.Builders;
using Elsa.Services;
using NodaTime;

namespace Elsa.Samples.Timers.Activities
{
    public class MyContainer2 : CompositeActivity
    {
        public override void Build(ICompositeActivityBuilder activity)
        {
            activity
                .WriteLine("In 2 seconds...")
                .Timer(Duration.FromSeconds(2))
                .WriteLine("The time is ripe.")
                .Then<Fork>(fork => fork.WithBranches("C", "D", "E"), fork =>
                {
                    fork.When("C")
                        .Then<MyContainer1>()
                        .Then("Join2");

                    fork
                        .When("D")
                        .While(true, @while => @while
                            .Timer(Duration.FromSeconds(1))
                            .WriteLine("Timer D went off"))
                        .Then("Join2");

                    fork
                        .When("E")
                        .Timer(Duration.FromSeconds(15))
                        .WriteLine("Timer E went off. Exiting fork.")
                        .Then("Join2");
                })
                .Add<Join>(join => join.WithMode(Join.JoinMode.WaitAny)).WithName("Join2")
                .WriteLine("Container 2 Joined!")
                .Finish();
        }
    }
}