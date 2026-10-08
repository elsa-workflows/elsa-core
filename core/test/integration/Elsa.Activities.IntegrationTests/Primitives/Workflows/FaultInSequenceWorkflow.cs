using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.Activities.IntegrationTests.Primitives.Workflows;

public class FaultInSequenceWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(FaultInSequenceWorkflow);

    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.DefinitionId = DefinitionId;
        workflow.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Before fault"),
                new Fault
                {
                    Code = new("SEQ_001"),
                    Category = new("Sequence"),
                    FaultType = new("Test"),
                    Message = new("Fault in sequence")
                },
                new WriteLine("After fault - should not execute")
            }
        };
    }
}