﻿using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Activities.Timers;
using Elsa.Builders;
using NodaTime;

namespace Elsa.Server.Host.Workflows
{
    public class ConditionWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .WithDisplayName("Conditions")
                .WriteLine("What is your age?")
                .ReadLine()
                .Timer(Duration.FromMinutes(5))
                .SetVariable("Age", context => int.Parse(context.GetInput<string>()))
                .IfElse(context => context.GetVariable<int>("Age") < 18, ifElse =>
                {
                    ifElse.When(OutcomeNames.True).WriteLine("You are not allowed to drink beer.");
                    ifElse.When(OutcomeNames.False).WriteLine("Enjoy your beer!");
                });
        }
    }
}