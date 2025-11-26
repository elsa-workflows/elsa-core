using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.Activities.IntegrationTests.Flow.Workflows;

/// <summary>
/// Workflow demonstrating that End activity completes successfully in a sequence.
/// </summary>
public class EndInSequenceWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.WithDefinitionId(DefinitionId);
        workflow.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Before End"),
                new End(),
                new WriteLine("After End")
            }
        };
    }
}
