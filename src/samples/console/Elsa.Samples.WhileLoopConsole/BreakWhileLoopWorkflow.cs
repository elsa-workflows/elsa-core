using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;
using Elsa.Services.Models;

namespace Elsa.Samples.WhileLoopConsole
{
    public class BreakWhileLoopWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WriteLine($"Looping {10} iterations.")
                .Then(context => SetCurrentCount(context, 1))
                .While(true,
                    @while =>
                    {
                        @while
                            .WriteLine(context => $"Counter: {GetCurrentCount(context)}")
                            .Then(context => SetCurrentCount(context, GetCurrentCount(context) + 1))
                            .IfTrue(context => GetCurrentCount(context) > 10, then => then.Break());
                    })
                .WriteLine("Finished");
        }

        private int GetCurrentCount(ActivityExecutionContext context) => context.GetVariable<int>("CurrentCount");
        private void SetCurrentCount(ActivityExecutionContext context, int value) => context.SetVariable("CurrentCount", value);
    }
}