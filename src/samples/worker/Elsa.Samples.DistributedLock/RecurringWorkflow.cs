using Elsa.Activities.Console;
using Elsa.Builders;
using Elsa.Models;
using NodaTime;
using System;
using Elsa.Activities.Timers;

namespace Elsa.Samples.DistributedLock
{
    public class RecurringWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .WithPersistenceBehavior(WorkflowPersistenceBehavior.ActivityExecuted)
                .Timer(Duration.FromSeconds(1))
                .WriteLine("Hello World")
                .WriteLine(() => $"Timer event at {DateTime.Now}");
        }
    }
}