using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.Activities.IntegrationTests.Primitives.Workflows;

public class FaultWithDefaultsWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(FaultWithDefaultsWorkflow);

    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.DefinitionId = DefinitionId;
        workflow.Root = new Fault
        {
            Code = new((string)null!),
            Category = new((string)null!),
            FaultType = new((string)null!),
            Message = new((string?)null)
        };
    }
}