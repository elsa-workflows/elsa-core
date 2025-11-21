using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Primitives.Event.Workflows;

/// <summary>
/// A workflow that listens for global events as a trigger.
/// </summary>
public class ConsumerWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.WithDefinitionId(DefinitionId);
        workflow.Root = new Sequence
        {
            Activities =
            {
                new Runtime.Activities.Event("GlobalOrderEvent")
                {
                    CanStartWorkflow = true
                },
                new End()
            }
        };
    }
}
