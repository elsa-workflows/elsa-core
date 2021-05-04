using System;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Builders;

namespace Elsa.Samples.BreakLoop.Workflows
{
    public class BreakoutWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WriteLine("Looping until we hit the break.")
                .While(true, @while =>
                {
                    @while
                        .SetVariable("LoopCount", context => context.GetVariable<int>("LoopCount") + 1)
                        .WriteLine(context => $"Iteration {context.GetVariable<int>("LoopCount")}. Hit break? (y/n):")
                        .ReadLine()
                        .IfTrue(context => string.Equals(context.GetInput<string>(), "y", StringComparison.OrdinalIgnoreCase), whenTrue => whenTrue
                            .WriteLine("Exiting infinite loop.")
                            .Break());
                })
                .WriteLine("Workflow finished.");
        }
    }
}