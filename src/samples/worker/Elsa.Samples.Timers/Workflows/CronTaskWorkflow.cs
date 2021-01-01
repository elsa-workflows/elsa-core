using System;
using Elsa.Activities.Console;
using Elsa.Activities.Timers;
using Elsa.Builders;

namespace Elsa.Samples.Timers.Workflows
{
    public class CronTaskWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .Cron("0/5 * * ? * * *")
                .WriteLine(() => $"CRON event at {DateTime.Now}");
        }
    }
}