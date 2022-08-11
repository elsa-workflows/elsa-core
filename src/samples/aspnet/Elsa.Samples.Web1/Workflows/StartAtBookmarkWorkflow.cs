using Elsa.Common.Services;
using Elsa.Scheduling.Activities;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Services;

namespace Elsa.Samples.Web1.Workflows;

public class StartAtBookmarkWorkflow : WorkflowBase
{
    private readonly ISystemClock _systemClock;

    public StartAtBookmarkWorkflow(ISystemClock systemClock)
    {
        _systemClock = systemClock;
    }

    protected override void Build(IWorkflowDefinitionBuilder workflow)
    {
        workflow.WithRoot(new Sequence
        {
            Activities =
            {
                new WriteLine("Waiting for 5 seconds..."),
                new StartAt(() => _systemClock.UtcNow.AddSeconds(5)),
                new WriteLine(() => $"Executed at {_systemClock.UtcNow}")
            }
        });
    }
}