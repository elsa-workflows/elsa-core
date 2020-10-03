using System;
using Elsa.Activities.Console;
using Elsa.Activities.MassTransit;
using Elsa.Builders;
using Elsa.Models;
using NodaTime;

namespace Elsa.Samples.Timers
{
    public class RecurringTaskWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WithPersistenceBehavior(WorkflowPersistenceBehavior.ActivityExecuted)
                .TimerEvent(Duration.FromSeconds(1))
                .WriteLine("Hello World")
                .WriteLine(() => $"Timer event at {DateTime.Now}");
        }
    }
}