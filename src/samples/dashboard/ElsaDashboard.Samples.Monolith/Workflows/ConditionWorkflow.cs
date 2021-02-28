using System;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Activities.Temporal;
using Elsa.Builders;
using NodaTime;

namespace ElsaDashboard.Samples.Monolith.Workflows
{
    public class ConditionWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WithDisplayName("Conditions")
                .Then(() => Console.WriteLine("What is your age?")).WithDisplayName("Write").WithDescription("What is your age?")
                .ReadLine()
                .Timer(Duration.FromMinutes(5))
                .SetVariable("Age", context => int.Parse(context.GetInput<string>()!))
                .If(
                    context => context.GetVariable<int>("Age") < 18, 
                    whenTrue => whenTrue.WriteLine("You are not allowed to drink beer."),
                    whenFalse => whenFalse.WriteLine("Enjoy your beer!"));
        }
    }
}