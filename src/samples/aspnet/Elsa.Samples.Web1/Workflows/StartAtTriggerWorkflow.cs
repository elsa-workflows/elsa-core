using System;
using Elsa.Scheduling.Activities;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Services;

namespace Elsa.Samples.Web1.Workflows;

public class StartAtTriggerWorkflow : WorkflowBase
{
    private readonly ISystemClock _systemClock;
    private readonly DateTimeOffset _executeAt;

    public StartAtTriggerWorkflow(ISystemClock systemClock)
    {
        _systemClock = systemClock;
        _executeAt = systemClock.UtcNow.AddSeconds(10);
    }

    protected override void Build(IWorkflowDefinitionBuilder workflow)
    {
        workflow.WithRoot(new Sequence
        {
            Activities =
            {
                new StartAt(_executeAt) { CanStartWorkflow = true },
                new WriteLine(() => $"Executed at {_systemClock.UtcNow}")
            }
        });
    }
}