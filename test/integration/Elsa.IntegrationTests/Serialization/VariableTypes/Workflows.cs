using Elsa.Workflows.Core.Abstractions;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.IntegrationTests.Serialization.VariableTypes;

class SampleWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.WithVariable<bool>();
    }
}