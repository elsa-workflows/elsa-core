using System;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Builders;
using Elsa.Expressions;

namespace Sample07
{
    using static Console;
    
    /// <summary>
    /// A simple flow chart where each activity is connected to the next.
    /// </summary>
    public class MyWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            var goodBye = new Inline(() => Console.WriteLine("Goodbye cruel world..."));

            builder
                .StartWith(() => Console.WriteLine("Step 0"))
                .If(() => true)
                .Then<IfElse>(x =>
                {
                    x.Condition = new CodeExpression<bool>(() => true);
                    x.True = new Inline(() => WriteLine("So true."));
                    x.False = new Inline(() => WriteLine("So not true."));
                })
                .Then(() => Console.WriteLine("Step 1"))
                .Then(() => Console.WriteLine("Step 2"))
                .Then(() => Console.WriteLine("Step 3"))
                .Then(goodBye);
        }
    }
}