using Elsa.Activities.Console;
using Elsa.Activities.MassTransit;
using Elsa.Builders;
using Elsa.Models;
using NodaTime;
using System;

namespace Elsa.Samples.DistributedLock
{
    public class RecurringWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .WithPersistenceBehavior(WorkflowPersistenceBehavior.ActivityExecuted)
                .TimerEvent(Duration.FromSeconds(1))
                .WriteLine("Hello World")
                .WriteLine(() => $"Timer event at {DateTime.Now}");
        }
    }
}