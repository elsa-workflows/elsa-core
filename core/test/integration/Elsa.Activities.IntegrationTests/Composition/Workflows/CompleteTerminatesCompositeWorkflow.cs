using Elsa.Activities.IntegrationTests.Composition.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.Activities.IntegrationTests.Composition.Workflows;

/// <summary>
/// Workflow demonstrating that Complete terminates composite execution immediately.
/// </summary>
public class CompleteTerminatesCompositeWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.WithDefinitionId(DefinitionId);
        workflow.Root = new Sequence
        {
            Activities =
            {
                new SimpleComposite(),
                new WriteLine("After composite")
            }
        };
    }
}