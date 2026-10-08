using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.Activities.IntegrationTests.Flow.Workflows;

/// <summary>
/// Workflow demonstrating that Start activity completes successfully in a sequence.
/// </summary>
public class StartInSequenceWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.WithDefinitionId(DefinitionId);
        workflow.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Before Start"),
                new Start(),
                new WriteLine("After Start")
            }
        };
    }
}
