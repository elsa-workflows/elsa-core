using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Primitives.Finish.Workflows;

/// <summary>
/// A workflow that publishes an event locally, then forks execution into two paths with Event activities,
/// then merges into a Finish activity. Tests completion callback clearance.
/// </summary>
public class FinishWithForkWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.WithDefinitionId(DefinitionId);

        workflow.Root = new Sequence
        {
            Activities =
            {
                new Start(),
                // Publish local event first
                new PublishEvent
                {
                    EventName = new("TriggerEvent"),
                    IsLocalEvent = new(true)
                },
                // Fork into two branches waiting for events
                new Fork
                {
                    JoinMode = ForkJoinMode.WaitAny,
                    Branches =
                    {
                        new Runtime.Activities.Event("TriggerEvent"),
                        new Runtime.Activities.Event("UnusedEvent")
                    }
                },
                // Finish clears remaining completion callbacks
                new Elsa.Workflows.Activities.Finish()
            }
        };
    }
}
