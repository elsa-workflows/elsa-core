using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.Activities.IntegrationTests.Primitives.Workflows;

public class FinishInSequenceWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(FinishInSequenceWorkflow);

    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.DefinitionId = DefinitionId;
        workflow.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Before Finish"),
                new Finish(),
                new WriteLine("After Finish")
            }
        };
    }
}
