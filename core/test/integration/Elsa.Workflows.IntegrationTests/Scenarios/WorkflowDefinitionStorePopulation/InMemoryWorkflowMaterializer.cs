using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowDefinitionStorePopulation;

public class InMemoryWorkflowMaterializer(Workflow workflow) : IWorkflowMaterializer
{
    public string Name => "InMemory";
    
    public ValueTask<Workflow> MaterializeAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var materializedWorkflow = new Workflow
        {
            Identity = new(definition.DefinitionId, definition.Version, definition.Id),
            Root = workflow.Root
        };
        return new(materializedWorkflow);
    }
}