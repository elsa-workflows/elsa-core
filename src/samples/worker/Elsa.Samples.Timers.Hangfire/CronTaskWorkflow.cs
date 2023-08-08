using System;
using Elsa.Activities.Console;
using Elsa.Activities.Temporal;
using Elsa.Builders;

namespace Elsa.Samples.Timers
{
    public class CronTaskWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .AsSingleton()
                .Cron("0/30 * * * * *")
                .WriteLine(() => $"CRON event at {DateTime.Now}");
        }
    }
}