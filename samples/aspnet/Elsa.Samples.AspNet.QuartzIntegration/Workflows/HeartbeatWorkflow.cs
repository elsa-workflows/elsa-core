using Elsa.Framework.System;
using Elsa.Scheduling.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Samples.AspNet.QuartzIntegration.Workflows;

[PublicAPI]
public class HeartbeatWorkflow(ISystemClock systemClock) : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new Cron
                {
                    CronExpression = new("*/1 * * * * ?"),
                    CanStartWorkflow = true
                },
                new WriteLine(new Input<string>($"Heartbeat workflow triggered at {systemClock.UtcNow.LocalDateTime}"))
            }
        };
    }
}