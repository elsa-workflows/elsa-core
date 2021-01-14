using System;
using Elsa.Activities.Console;
using Elsa.Activities.Timers;
using Elsa.Builders;
using NodaTime;

namespace Elsa.Samples.Faulting.Workflows
{
    public class FaultyWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .StartIn(Duration.FromSeconds(1))
                .WriteLine("Catch this!")
                .Then(() => throw new ArithmeticException("Does not compute"));
        }
    }
}