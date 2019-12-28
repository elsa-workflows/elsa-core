using System;
using Elsa.Activities;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.ControlFlow.Activities;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Sample25
{
    public class ForLoopDemoWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            const int start = 0;
            const int end = 3;

            builder
                .StartWith(() => Console.WriteLine("Welcome to the For activity demo!"))
                .Then(() => Console.WriteLine($"Iterating between {start} and {end}"))
                .For(start, end, new CodeActivity(context => Console.WriteLine($"Iteration {context.Input.GetValue<int>()}")))
                .Then<WriteLine>(x => x.Text = new LiteralExpression<string>("End of for loop."));
        }
    }
}