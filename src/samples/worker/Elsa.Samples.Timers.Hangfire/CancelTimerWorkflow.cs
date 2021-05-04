using System.Collections.Generic;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Temporal;
using Elsa.Builders;
using NodaTime;

namespace Elsa.Samples.Timers
{
    public class CancelTimerWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartAt(SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromSeconds(5)))
                .WriteLine("CancelTimerWorkflow is executed")
                .Then<Fork>(
                    activity => activity.Set(x => x.Branches, new HashSet<string>(new[] { "Branch 1", "Branch 2" })),
                    fork =>
                    {
                        fork.When("Branch 1").Timer(Duration.FromSeconds(30)).WithId("timer-1").WriteLine("Should not be executed");
                        fork.When("Branch 2").Timer(Duration.FromSeconds(10))
                            .CancelTimer("timer-1")
                            .WriteLine("Timer-1 was canceled")
                            .Timer(Duration.FromSeconds(40))
                            .WriteLine("CancelTimerWorkflow finished")
                            .Finish();
                    });
        }
    }
}