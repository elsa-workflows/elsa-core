using System;
using Elsa.Activities.Console;
using Elsa.Activities.Temporal;
using Elsa.Builders;
using NodaTime;

namespace Elsa.Samples.Timers.Workflows
{
    public class SingletonTimerWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .AsSingleton()
                .Timer(Duration.FromSeconds(1))
                .WriteLine(context => $"{context.WorkflowInstance.Id} - Timer event at {DateTime.Now}. Waiting for a couple of seconds.")
                .Timer(Duration.FromSeconds(2))
                .WriteLine(context => $"{context.WorkflowInstance.Id} - Resuming at {DateTime.Now}.")
                .Timer(Duration.FromSeconds(1))
                .WriteLine(context => $"{context.WorkflowInstance.Id} - Finished.");
        }
    }
}