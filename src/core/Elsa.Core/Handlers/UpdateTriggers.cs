using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Elsa.Handlers;

public class UpdateTriggers : 
    INotificationHandler<WorkflowDefinitionPublished>,
    INotificationHandler<WorkflowDefinitionDeleted>,
    INotificationHandler<WorkflowDefinitionRetracted>,
    INotificationHandler<ManyWorkflowDefinitionsDeleted>
{
    private readonly ITriggerIndexer _triggerIndexer;
    private readonly IWorkflowBlueprintMaterializer _workflowBlueprintMaterializer;
    private readonly ILogger _logger;

    public UpdateTriggers(ITriggerIndexer triggerIndexer, IWorkflowBlueprintMaterializer workflowBlueprintMaterializer, ILogger<UpdateTriggers> logger)
    {
        _triggerIndexer = triggerIndexer;
        _workflowBlueprintMaterializer = workflowBlueprintMaterializer;
        _logger = logger;
    }

    public async Task Handle(WorkflowDefinitionPublished notification, CancellationToken cancellationToken) => await IndexTriggersAsync(notification.WorkflowDefinition, cancellationToken);

    public async Task Handle(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken)
    {
        var workflowDefinition = notification.WorkflowDefinition;
        await _triggerIndexer.DeleteTriggersAsync(workflowDefinition.DefinitionId, cancellationToken);
    }

    public async Task Handle(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken)
    {
        var workflowDefinition = notification.WorkflowDefinition;
        await _triggerIndexer.DeleteTriggersAsync(workflowDefinition.DefinitionId, cancellationToken);
    }

    public async Task Handle(ManyWorkflowDefinitionsDeleted notification, CancellationToken cancellationToken)
    {
        foreach (var workflowDefinition in notification.WorkflowDefinitions) 
            await _triggerIndexer.DeleteTriggersAsync(workflowDefinition.DefinitionId, cancellationToken);
    }
    
    private async Task IndexTriggersAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken)
    {
        var workflowBlueprint = await TryMaterializeBlueprintAsync(workflowDefinition, cancellationToken);
        
        if(workflowBlueprint != null)
            await _triggerIndexer.IndexTriggersAsync(workflowBlueprint, cancellationToken);
    }
    
    private async Task<IWorkflowBlueprint?> TryMaterializeBlueprintAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken)
    {
        try
        {
            return await _workflowBlueprintMaterializer.CreateWorkflowBlueprintAsync(workflowDefinition, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Failed to materialize workflow definition {WorkflowDefinitionId} with version {WorkflowDefinitionVersion}", workflowDefinition.DefinitionId, workflowDefinition.Version);
        }

        return null;
    }
}