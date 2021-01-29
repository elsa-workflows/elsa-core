using System;
using Elsa.Activities.Console;
using Elsa.Activities.Timers;
using Elsa.Builders;
using NodaTime;

namespace Elsa.Samples.Timers.Workflows
{
    public class TimerWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .Timer(Duration.FromSeconds(1))
                .WriteLine(() => $"Timer event at {DateTime.Now}");
        }
    }
}