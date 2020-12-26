using System;
using Elsa.Activities.Console;
using Elsa.Activities.Timers;
using Elsa.Builders;

namespace Elsa.Samples.Timers
{
    public class CronTaskWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .Cron("0/30 * * * * *")
                .WriteLine(() => $"CRON event at {DateTime.Now}");
        }
    }
}