using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Temporal;
using Elsa.Builders;
using Elsa.Services;
using NodaTime;

namespace Elsa.Samples.Timers.Activities
{
    public class MyContainer1 : CompositeActivity
    {
        public override void Build(ICompositeActivityBuilder builder)
        {
            builder
                .StartWith<Fork>(fork => fork.WithBranches("A", "B", "C"), fork =>
                {
                    fork
                        .When("A")
                        .While(true, @while => @while
                            .Timer(Duration.FromSeconds(1))
                            .WriteLine("Timer A went off"))
                        .ThenNamed("Join1");

                    fork
                        .When("B")
                        .While(true, @while => @while
                            .Timer(Duration.FromSeconds(5))
                            .WriteLine("Timer B went off"))
                        .ThenNamed("Join1");

                    fork
                        .When("C")
                        .Timer(Duration.FromSeconds(6))
                        .WriteLine("Timer C went off")
                        .ThenNamed("Join1");
                })
                .Add<Join>(join => join.WithMode(Join.JoinMode.WaitAny)).WithName("Join1")
                .WriteLine("Container 1 Joined!")
                .Finish();
        }
    }
}