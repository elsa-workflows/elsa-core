using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowDefinitionStorePopulation;

public class InMemoryWorkflowsProvider(Workflow workflow) : IWorkflowsProvider
{
    public string Name => "InMemory";

    public ValueTask<IEnumerable<MaterializedWorkflow>> GetWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        var materializedWorkflow = new MaterializedWorkflow(
            Workflow: workflow,
            ProviderName: "InMemory",
            MaterializerName: "InMemory"
        );

        return new([materializedWorkflow]);
    }
}