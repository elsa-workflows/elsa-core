using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.Activities.IntegrationTests.Primitives.Workflows;

public class FaultWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(FaultWorkflow);

    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.DefinitionId = DefinitionId;
        workflow.Root = new Fault
        {
            Code = new("ERR_001"),
            Category = new("Test"),
            FaultType = new("Business"),
            Message = new("Test fault message")
        };
    }
}