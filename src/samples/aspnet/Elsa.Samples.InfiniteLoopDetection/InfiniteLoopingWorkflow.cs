using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;

namespace Elsa.Samples.InfiniteLoopDetection
{
    /// <summary>
    /// A workflow that is triggered when HTTP requests are made to /hello and writes a response.
    /// </summary>
    public class InfiniteLoopingWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .SetVariable("Count", 0)
                .While(true, iteration => iteration.WriteLine(context =>
                {
                    var count = context.SetVariable<int>("Count", x => x + 1);
                    return $"Iteration {count}";
                }))
                .WriteLine("This will never execute");
        }
    }
}