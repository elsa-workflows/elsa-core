using System;
using Elsa.Activities.Console;
using Elsa.Activities.MassTransit;
using Elsa.Builders;
using NodaTime;

namespace Elsa.Samples.Timers
{
    public class CronTaskWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .CronEvent("* * * * *")
                .WriteLine(() => $"CRON event at {DateTime.Now}");
        }
    }
}