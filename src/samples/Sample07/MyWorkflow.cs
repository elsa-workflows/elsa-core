using System;
using System.Linq;
using Elsa.Activities.Console.Activities;
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
            builder
                .For(-15, 15, 5, iteration => iteration.Then(context => WriteLine($"Value: {context.Input.Value}")))
                .For(-15, 15, 5, iteration => iteration.Then<WriteLine>(writeLine => writeLine.With(x => x.Text, new CodeExpression<string>(e => $"Value: {e.Input.Value}"))))
                .SetVariable("Counter", () => 0)
                .While(
                    context => context.GetCounter() < 10, 
                    iteration => iteration
                        .Then(context => WriteLine($"Counter: {context.GetCounter()}"))
                        .SetVariable<int>("Counter", current => current + 1))
                .Then(context => WriteLine($"Your lucky number is: {context.GetCounter()}"));
        }
    }
}