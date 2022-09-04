using Elsa.Models;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Implementations;

public class WorkflowDefinitionService : IWorkflowDefinitionService
{
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IEnumerable<IWorkflowMaterializer> _materializers;

    public WorkflowDefinitionService(IWorkflowDefinitionStore workflowDefinitionStore, IEnumerable<IWorkflowMaterializer> materializers)
    {
        _workflowDefinitionStore = workflowDefinitionStore;
        _materializers = materializers;
    }

    public async Task<Workflow> MaterializeWorkflowAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var provider = _materializers.FirstOrDefault(x => x.Name == definition.MaterializerName);

        if (provider == null)
            throw new Exception("Provider not found");

        return await provider.MaterializeAsync(definition, cancellationToken);
    }

    public async Task<WorkflowDefinition?> FindAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default) =>
        await _workflowDefinitionStore.FindByDefinitionIdAsync(definitionId, versionOptions, cancellationToken);
}