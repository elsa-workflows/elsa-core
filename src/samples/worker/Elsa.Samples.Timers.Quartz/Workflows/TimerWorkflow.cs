using System;
using Elsa.Activities.Console;
using Elsa.Activities.Temporal;
using Elsa.Builders;
using NodaTime;

namespace Elsa.Samples.Timers.Workflows
{
    public class TimerWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .Timer(Duration.FromSeconds(1))
                .WriteLine(() => $"Timer event at {DateTime.Now}");
        }
    }
}