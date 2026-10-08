using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.Activities.IntegrationTests.Primitives.Workflows;

public class FaultViaFactoryWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(FaultViaFactoryWorkflow);

    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.DefinitionId = DefinitionId;
        workflow.Root = Fault.Create("FACTORY_001", "Factory", "Integration", "Created via factory");
    }
}