using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Timers;
using Elsa.Builders;
using Elsa.Services;
using NodaTime;

namespace Elsa.Samples.Timers
{
    public class MyContainer2 : CompositeActivity
    {
        public override void Build(ICompositeActivityBuilder activity)
        {
            activity
                .StartIn(Duration.FromSeconds(5))
                .Then<Fork>(fork => fork.WithBranches("A", "B"), fork =>
                {
                    fork
                        .When("A")
                        .While(true, @while => @while
                            .Timer(Duration.FromSeconds(5))
                            .WriteLine("Timer C went off"))
                        .Then("Join2");

                    fork
                        .When("B")
                        .While(true, @while => @while
                            .Timer(Duration.FromSeconds(5))
                            .WriteLine("Timer D went off"))
                        .Then("Join2");
                })
                .Add<Join>(join => join.WithMode(Join.JoinMode.WaitAny)).WithName("Join2").WriteLine("Container 2 Joined!");
        }
    }
}