using System;
using Elsa.Activities.Console;
using Elsa.Activities.MassTransit;
using Elsa.Builders;
using NodaTime;

namespace Elsa.Samples.Timers
{
    public class RecurringTaskWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .TimerEvent(Duration.FromSeconds(5))
                .WriteLine(() => $"Timer event at {DateTime.Now}");
        }
    }
}