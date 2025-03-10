using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Materializers;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class DefaultWorkflowRegistry(IWorkflowDefinitionStorePopulator populator) : IWorkflowRegistry
{
    /// <inheritdoc />
    public async Task RegisterAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        var materializedWorkflow = new MaterializedWorkflow(workflow, nameof(DefaultWorkflowRegistry), TypedWorkflowMaterializer.MaterializerName);
        await populator.AddAsync(materializedWorkflow, cancellationToken);
    }
}