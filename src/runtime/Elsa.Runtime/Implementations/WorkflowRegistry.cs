using Elsa.Mediator.Services;
using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;
using Elsa.Runtime.Services;

namespace Elsa.Runtime.Implementations;

public class WorkflowRegistry : IWorkflowRegistry
{
    private readonly IRequestSender _requestSender;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;

    public WorkflowRegistry(IRequestSender requestSender, IWorkflowDefinitionService workflowDefinitionService)
    {
        _requestSender = requestSender;
        _workflowDefinitionService = workflowDefinitionService;
    }

    public async Task<Workflow?> FindByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var definition = await _requestSender.RequestAsync(new FindWorkflowDefinitionByDefinitionId(definitionId, versionOptions), cancellationToken);
        return await MaterializeAsync(definition, cancellationToken);
    }

    public async Task<Workflow?> FindByNameAsync(string name, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var definition = await _requestSender.RequestAsync(new FindWorkflowDefinitionByName(name, versionOptions), cancellationToken);
        return await MaterializeAsync(definition, cancellationToken);
    }

    private async Task<Workflow?> MaterializeAsync(WorkflowDefinition? definition, CancellationToken cancellationToken)
    {
        if (definition == null)
            return null;
        
        return await _workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
    }
}