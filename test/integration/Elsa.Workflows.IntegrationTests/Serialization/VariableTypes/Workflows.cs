using Elsa.Workflows.Contracts;

namespace Elsa.Workflows.IntegrationTests.Serialization.VariableTypes;

class SampleWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.WithVariable<bool>();
    }
}