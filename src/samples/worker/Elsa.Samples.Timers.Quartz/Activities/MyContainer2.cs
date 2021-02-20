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
                .StartWith<Fork>(fork => fork.WithBranches("D", "E"), fork =>
                {
                    fork
                        .When("D")
                        .Timer(Duration.FromSeconds(20))
                        .WriteLine("Timer D went off. Exiting fork")
                        .Then("Join2");

                    fork
                        .When("E")
                        .While(true, iterate => iterate
                            .Timer(Duration.FromSeconds(5))
                            .WriteLine("Timer E went off. Looping."))
                        .Then("Join2");
                })
                .Add<Join>(join => join.WithMode(Join.JoinMode.WaitAny)).WithName("Join2")
                .WriteLine("Container 2 Joined!")
                .Finish();
        }
    }
}