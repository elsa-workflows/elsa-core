using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Models;
using Elsa.Persistence.Services;
using Elsa.Runtime.Services;

namespace Elsa.Runtime.Implementations;

public class WorkflowRegistry : IWorkflowRegistry
{
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;

    public WorkflowRegistry(IWorkflowDefinitionStore workflowDefinitionStore, IWorkflowDefinitionService workflowDefinitionService)
    {
        _workflowDefinitionStore = workflowDefinitionStore;
        _workflowDefinitionService = workflowDefinitionService;
    }

    public async Task<Workflow?> FindByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var definition = await _workflowDefinitionStore.FindByDefinitionIdAsync(definitionId, versionOptions, cancellationToken);
        return await MaterializeAsync(definition, cancellationToken);
    }

    public async Task<Workflow?> FindByNameAsync(string name, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var definition = await _workflowDefinitionStore.FindByNameAsync(name, versionOptions, cancellationToken);
        return await MaterializeAsync(definition, cancellationToken);
    }

    private async Task<Workflow?> MaterializeAsync(WorkflowDefinition? definition, CancellationToken cancellationToken)
    {
        if (definition == null)
            return null;
        
        return await _workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
    }
}