using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Builders;

namespace Elsa.Samples.RunChildWorkflowWorker.Workflows
{
    public class ChildWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .SetVariable("Count", context => (long)context.Input!)
                .WriteLine(context => $"Child workflow counting down from {context.GetVariable<long>("Count")} to 0")
                .For(
                    context => context.GetVariable<long>("Count"),
                    _ => 0,
                    _ => -1,
                    iterate => { iterate.WriteLine(context => $"{context.Input}"); },
                    Operator.GreaterThanOrEqual)
                .WriteLine("Done. Back to you, parent workflow!");
        }
    }
}