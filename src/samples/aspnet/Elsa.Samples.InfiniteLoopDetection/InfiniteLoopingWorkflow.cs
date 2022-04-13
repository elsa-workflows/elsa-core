using Elsa.Activities.Primitives;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;

namespace Elsa.Samples.InfiniteLoopDetection
{
    /// <summary>
    /// A workflow that never ends. Except when the infinite loop is detected by the runtime. 
    /// </summary>
    public class InfiniteLoopingWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .SetVariable(name: "Count", 0)
                .While(true, iteration => iteration.WriteLine(context =>
                {
                    var count = context.SetVariable<int>("Count", x => x + 1);
                    return $"Iteration {count}";
                }))
                .WriteLine("This will never execute");
        }
    }
}