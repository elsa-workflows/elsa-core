using Elsa.Common.Models;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Implementations;

/// <inheritdoc />
public class WorkflowDefinitionService : IWorkflowDefinitionService
{
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IIdentityGraphService _identityGraphService;
    private readonly IEnumerable<IWorkflowMaterializer> _materializers;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowDefinitionService(IWorkflowDefinitionStore workflowDefinitionStore, IIdentityGraphService identityGraphService, IEnumerable<IWorkflowMaterializer> materializers)
    {
        _workflowDefinitionStore = workflowDefinitionStore;
        _identityGraphService = identityGraphService;
        _materializers = materializers;
    }

    /// <inheritdoc />
    public async Task<Workflow> MaterializeWorkflowAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var provider = _materializers.FirstOrDefault(x => x.Name == definition.MaterializerName);

        if (provider == null)
            throw new Exception("Provider not found");

        var workflow = await provider.MaterializeAsync(definition, cancellationToken);
        
        // Assign identities.
        await _identityGraphService.AssignIdentitiesAsync(workflow, cancellationToken);

        return workflow;
    }
    
    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default) =>
        await _workflowDefinitionStore.FindByDefinitionIdAsync(definitionId, versionOptions, cancellationToken);
}