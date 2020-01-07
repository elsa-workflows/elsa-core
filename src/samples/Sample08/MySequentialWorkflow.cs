using System;
using Elsa.Activities.Primitives;
using Elsa.Builders;

namespace Sample08
{
    /// <summary>
    /// A simple sequence diagram where each activity is automatically scheduled by the Sequence activity.
    /// </summary>
    public class MySequentialWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            var goodBye = new Inline(() => Console.WriteLine("Goodbye cruel world..."));

            // builder.BuildSequence()
            //     .Add(() => Console.WriteLine("Step 0"))
            //     .Add(() => Console.WriteLine("Step 1"))
            //     .Add(() => Console.WriteLine("Step 2"))
            //     .Add(() => Console.WriteLine("Step 3"))
            //     .Add(goodBye);
        }
    }
}