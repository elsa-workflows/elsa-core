using Elsa.Common.Contracts;
using Elsa.Scheduling.Activities;
using Elsa.Workflows.Core.Abstractions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
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
                new WriteLine
                {
                    Text = new($"Heartbeat workflow triggered at {_systemClock.UtcNow.LocalDateTime}")
                }
            }
        };
    }
}