using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Builders;

namespace Sample07
{
    /// <summary>
    /// A simple workflow where each activity is connected to the next.
    /// </summary>
    public class MyWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .SetVariable("Counter", 0)
                .While(x => x.GetCounter() <= 10, whileTrue => whileTrue
                    .WriteLine(x => $"Counter: {x.GetCounter()}")
                    .SetVariable<int>("Counter", current => current + 1)
                )
                .WriteLine("Done");
        }
    }
}