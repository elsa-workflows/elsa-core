using Elsa.Workflows.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Primitives.Finish.Workflows;

/// <summary>
/// A simple workflow that executes Finish activity.
/// </summary>
public class SimpleFinishWorkflow : WorkflowBase
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
                new Elsa.Workflows.Activities.Finish()
            }
        };
    }
}
