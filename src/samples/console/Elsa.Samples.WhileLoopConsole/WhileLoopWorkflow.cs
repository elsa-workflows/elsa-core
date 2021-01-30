using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;
using Elsa.Services.Models;

namespace Elsa.Samples.WhileLoopConsole
{
    public class WhileLoopWorkflow : IWorkflow
    {
        private readonly int _counter;

        public WhileLoopWorkflow(int counter)
        {
            _counter = counter;
        }
        
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WriteLine($"Looping {_counter} iterations.")
                .Then(context => SetCurrentCount(context, _counter))
                .While(
                    context => GetCurrentCount(context) > 0,
                    @while =>
                    {
                        @while
                            .WriteLine(context => $"Counter: {GetCurrentCount(context)}")
                            .Then(context => SetCurrentCount(context, GetCurrentCount(context) - 1));
                    })
                .WriteLine("Finished");
        }

        private int GetCurrentCount(ActivityExecutionContext context) => context.GetVariable<int>("CurrentCount");
        private void SetCurrentCount(ActivityExecutionContext context, int value) => context.SetVariable("CurrentCount", value);
    }
}