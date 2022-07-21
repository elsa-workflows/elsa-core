using System.Text.Json;
using Elsa.ActivityDefinitions.Activities;
using Elsa.ActivityDefinitions.Extensions;
using Elsa.ActivityDefinitions.Services;
using Elsa.Mediator.Services;
using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;

namespace Elsa.ActivityDefinitions.Handlers;

/// <summary>
/// Updates the referenced definition version for all activity definitions referenced by the published workflow definition.
/// </summary>
public class UpdateReferencedVersionHandler : INotificationHandler<WorkflowDefinitionPublishing>
{
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IActivityWalker _activityWalker;
    private readonly IActivityDefinitionStore _activityDefinitionStore;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;
    private readonly ILogger<UpdateReferencedVersionHandler> _logger;

    public UpdateReferencedVersionHandler(
        IWorkflowDefinitionService workflowDefinitionService, 
        IActivityWalker activityWalker, 
        IActivityDefinitionStore activityDefinitionStore, 
        SerializerOptionsProvider serializerOptionsProvider,
        ILogger<UpdateReferencedVersionHandler> logger)
    {
        _workflowDefinitionService = workflowDefinitionService;
        _activityWalker = activityWalker;
        _activityDefinitionStore = activityDefinitionStore;
        _serializerOptionsProvider = serializerOptionsProvider;
        _logger = logger;
    }

    public async Task HandleAsync(WorkflowDefinitionPublishing notification, CancellationToken cancellationToken)
    {
        var workflowDefinition = notification.WorkflowDefinition;
        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        var root = workflow.Root;
        var graph = await _activityWalker.WalkAsync(root, cancellationToken);
        var nodes = graph.Flatten().Distinct().ToList();
        var activityDefinitionLookup = await _activityDefinitionStore.ListAsync(VersionOptions.Published, cancellationToken).ToDictionary(x => x.DefinitionId);
        var customActivities = nodes.Where(x => x.Activity is ActivityDefinitionActivity).Select(x => (ActivityDefinitionActivity)x.Activity).ToList();

        foreach (var customActivity in customActivities)
        {
            if(!activityDefinitionLookup.TryGetValue(customActivity.DefinitionId, out var activityDefinition))
            {
                _logger.LogWarning(
                    "Workflow definition {WorkflowDefinitionId} version {WorkflowDefinitionVersion} references a custom activity with ID {ActivityDefinitionId}, but there is no (published) activity definition by that ID",
                    workflowDefinition.DefinitionId,
                    workflowDefinition.Version,
                    customActivity.DefinitionId);
                
                continue;
            }

            // Update to latest published version.
            customActivity.DefinitionVersion = activityDefinition.Version;
        }
        
        // Serialize the workflow and save changes.
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        workflowDefinition.StringData = JsonSerializer.Serialize(root, serializerOptions);
    }
}