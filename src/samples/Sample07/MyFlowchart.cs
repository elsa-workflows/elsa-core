using System;
using Elsa.Activities.Primitives;
using Elsa.Builders;

namespace Sample07
{
    /// <summary>
    /// A simple flow chart where each activity is connected to the next.
    /// </summary>
    public class MyFlowchart : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            var goodBye = new Inline(() => Console.WriteLine("Goodbye cruel world..."));

            builder.BuildFlowchart()
                .StartWith(() => Console.WriteLine("Step 0"))
                .Then(() => Console.WriteLine("Step 1"))
                .Then(() => Console.WriteLine("Step 2"))
                .Then(() => Console.WriteLine("Step 3"))
                .Then(goodBye);
        }
    }
}