using System;
using Elsa.Activities;
using Elsa.Modules.Activities.Console;
using Elsa.Modules.Scheduling.Activities;
using Elsa.Services;

namespace Elsa.Samples.Web1.Workflows;

public class StartAtTriggerWorkflow : IWorkflow
{
    private readonly ISystemClock _systemClock;
    private readonly DateTimeOffset _executeAt;

    public StartAtTriggerWorkflow(ISystemClock systemClock)
    {
        _systemClock = systemClock;
        _executeAt = systemClock.UtcNow.AddSeconds(10);
    }

    public void Build(IWorkflowDefinitionBuilder workflow)
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