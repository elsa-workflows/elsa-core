using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Primitives;
using Elsa.Activities.Temporal;
using Elsa.Builders;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Samples.ForkJoinTimerAndSignalHttp.Workflows
{
    /// <summary>
    /// Demonstrates the Fork, Join, Timer and Signal activities working together to model a long-running process where either the timer causes the workflow to resume, or a signal. 
    /// </summary>
    public class DemoWorkflow : IWorkflow
    {
        private readonly IClock _clock;
        private readonly Duration _timeOut;

        public DemoWorkflow(IClock clock)
        {
            _clock = clock;
            _timeOut = Duration.FromSeconds(20);
        }

        public void Build(IWorkflowBuilder builder)
        {
            builder
                .SetVariable("SignalUrl", context => context.GenerateSignalUrl("hurry"))
                .WriteLine(context =>
                    $"The demo completes in {_timeOut.ToString()} ({_clock.GetCurrentInstant().Plus(_timeOut)}). Can't wait that long? Send me the secret \"hurry\" signal! ({context.GetVariable<string>("SignalUrl")})")
                .Then<Fork>(
                    fork => fork.WithBranches("Timer", "Signal"),
                    fork =>
                    {
                        fork
                            .When("Timer")
                            .Timer(_timeOut)
                            .SetVariable("CompletedVia", "Timer")
                            .ThenNamed("Join");

                        fork
                            .When("Signal")
                            .SignalReceived("hurry")
                            .SetVariable("CompletedVia", "Signal")
                            .ThenNamed("Join");
                    })
                .Add<Join>(x => x.WithMode(Join.JoinMode.WaitAny)).WithName("Join")
                .WriteLine(context => $"Demo {GetCorrelationId(context)} completed successfully via {context.GetVariable<string>("CompletedVia")}!");
        }

        private string GetCorrelationId(ActivityExecutionContext context) => context.WorkflowExecutionContext.CorrelationId;
    }
}