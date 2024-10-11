using Elsa.Common;
using Elsa.Scheduling.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Samples.AspNet.QuartzIntegration.Workflows;

[PublicAPI]
public class HeartbeatWorkflow : WorkflowBase
{
    private readonly ISystemClock _systemClock;

    public HeartbeatWorkflow(ISystemClock systemClock)
    {
        _systemClock = systemClock;
    }

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
                new WriteLine(new Input<string>($"Heartbeat workflow triggered at {_systemClock.UtcNow.LocalDateTime}"))
            }
        };
    }
}