using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Primitives.Finish.Workflows;

/// <summary>
/// A workflow with multiple Event branches and a Finish activity.
/// Tests that Finish clears all scheduled work when executed.
/// </summary>
public class FinishWithMultipleBookmarksWorkflow : WorkflowBase
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
                // Publish local event that one branch will receive
                new PublishEvent
                {
                    EventName = new("EventA"),
                    IsLocalEvent = new(true)
                },
                new Fork
                {
                    JoinMode = ForkJoinMode.WaitAny,
                    Branches =
                    {
                        new Runtime.Activities.Event("EventA"),
                        new Runtime.Activities.Event("EventB"),
                        new Runtime.Activities.Event("EventC")
                    }
                },
                new Elsa.Workflows.Activities.Finish()
            }
        };
    }
}
