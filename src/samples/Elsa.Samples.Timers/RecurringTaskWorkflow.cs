using System;
using Elsa.Activities.Console;
using Elsa.Activities.MassTransit;
using Elsa.Builders;

namespace Elsa.Samples.Timers
{
    public class RecurringTaskWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .TimerEvent(TimeSpan.FromSeconds(5))
                .WriteLine(() => $"Timer event at {DateTime.Now}");
        }
    }
}