using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Notifications;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowDefinitionManager : IWorkflowDefinitionManager
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly INotificationSender _notificationSender;
    private readonly IWorkflowDefinitionPublisher _workflowPublisher;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly IActivitySerializer _activitySerializer;
    private readonly IActivityVisitor _activityVisitor;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowDefinitionManager(
        IWorkflowDefinitionStore store,
        INotificationSender notificationSender,
        IWorkflowDefinitionPublisher workflowPublisher,
        IIdentityGenerator identityGenerator,
        IActivitySerializer activitySerializer,
        IActivityVisitor activityVisitor)
    {
        _store = store;
        _notificationSender = notificationSender;
        _workflowPublisher = workflowPublisher;
        _identityGenerator = identityGenerator;
        _activitySerializer = activitySerializer;
        _activityVisitor = activityVisitor;
    }

    /// <inheritdoc />
    public async Task<long> DeleteByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        await _notificationSender.SendAsync(new WorkflowDefinitionDeleting(definitionId), cancellationToken);
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId };
        var count = await _store.DeleteAsync(filter, cancellationToken);
        await EnsureLastVersionIsLatestAsync(definitionId, cancellationToken);
        await _notificationSender.SendAsync(new WorkflowDefinitionDeleted(definitionId), cancellationToken);
        return count;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter { Id = id};
        var definition = await _store.FindAsync(filter, cancellationToken);
        
        if (definition == null)
            return false;
        
        return await DeleteVersionAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> BulkDeleteByDefinitionIdsAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default)
    {
        var definitionIdList = definitionIds.Distinct().ToList();
        await _notificationSender.SendAsync(new WorkflowDefinitionsDeleting(definitionIdList), cancellationToken);
        var filter = new WorkflowDefinitionFilter { DefinitionIds = definitionIdList };
        var count = await _store.DeleteAsync(filter, cancellationToken);
        await EnsureLastVersionIsLatestAsync(definitionIdList, cancellationToken);
        await _notificationSender.SendAsync(new WorkflowDefinitionsDeleted(definitionIdList), cancellationToken);
        return count;
    }

    /// <inheritdoc />
    public async Task<long> BulkDeleteByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        var definitions = await _store.FindSummariesAsync(new WorkflowDefinitionFilter { Ids = idList }, cancellationToken);
        var definitionIds = definitions.Select(x => x.DefinitionId).Distinct().ToList();
        await _notificationSender.SendAsync(new WorkflowDefinitionVersionsDeleting(idList), cancellationToken);
        var filter = new WorkflowDefinitionFilter { Ids = idList };
        var count = await _store.DeleteAsync(filter, cancellationToken);
        await EnsureLastVersionIsLatestAsync(definitionIds, cancellationToken);
        await _notificationSender.SendAsync(new WorkflowDefinitionVersionsDeleted(idList), cancellationToken);
        return count;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteVersionAsync(string definitionId, int versionToDelete, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = VersionOptions.SpecificVersion(versionToDelete) };
        var definitionToDelete = await _store.FindAsync(filter, cancellationToken);
        
        if (definitionToDelete == null)
            return false;
        
        return await DeleteVersionAsync(definitionToDelete, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition> RevertVersionAsync(string definitionId, int version, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = VersionOptions.Latest };
        var latestVersion = await _store.FindAsync(filter, cancellationToken);
        
        if(latestVersion != null)
        {
            latestVersion.IsLatest = false;
            await _store.SaveAsync(latestVersion, cancellationToken);
        }

        var draft = await _workflowPublisher.GetDraftAsync(definitionId, VersionOptions.SpecificVersion(version), cancellationToken);
        draft!.Id = _identityGenerator.GenerateId();
        draft.Version = (latestVersion?.Version ?? 0) + 1;
        draft.IsLatest = true;

        await _store.SaveAsync(draft, cancellationToken);
        return draft;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> UpdateReferencesInConsumingWorkflows(WorkflowDefinition dependency, CancellationToken cancellationToken = default)
    {
        var updatedWorkflowDefinitions = new List<WorkflowDefinition>();
        
        var publishedWorkflowDefinitions = (await _store.FindManyAsync(new WorkflowDefinitionFilter
        {
            VersionOptions = VersionOptions.Published
        }, cancellationToken)).ToList();

        foreach (var definition in publishedWorkflowDefinitions)
        {
            var root = _activitySerializer.Deserialize(definition.StringData!);
            var graph = await _activityVisitor.VisitAsync(root, cancellationToken);
            var flattenedList = graph.Flatten().ToList();
            var definitionId = dependency.DefinitionId;
            var version = dependency.Version;
            var nodes = flattenedList
                .Where(x => x.Activity is WorkflowDefinitionActivity workflowDefinitionActivity && workflowDefinitionActivity.WorkflowDefinitionId == definitionId)
                .ToList();

            foreach (var node in nodes.Where(activity => activity.Activity.Version < version))
            {
                var activity = (WorkflowDefinitionActivity) node.Activity;
                activity.Version = version;
                activity.WorkflowDefinitionVersionId = dependency.Id;

                if (!updatedWorkflowDefinitions.Contains(definition))
                    updatedWorkflowDefinitions.Add(definition);
            }

            if (updatedWorkflowDefinitions.Contains(definition))
            {
                var serializedData = _activitySerializer.Serialize(root);
                definition.StringData = serializedData;
            }
        }
        
        if(updatedWorkflowDefinitions.Any())
            await _store.SaveManyAsync(updatedWorkflowDefinitions, cancellationToken);

        return updatedWorkflowDefinitions;
    }
    
    private async Task<bool> DeleteVersionAsync(WorkflowDefinition definitionToDelete, CancellationToken cancellationToken)
    {
        if (definitionToDelete.IsPublished)
        {
            throw new Exception("Published version cannot be deleted before retracting it");
        }
        
        await _notificationSender.SendAsync(new WorkflowDefinitionVersionDeleting(definitionToDelete), cancellationToken);
        
        var filter = new WorkflowDefinitionFilter { Id = definitionToDelete.Id };
        var isDeleted = await _store.DeleteAsync(filter, cancellationToken) > 0;

        if (!isDeleted)
            return false;

        await EnsureLastVersionIsLatestAsync(definitionToDelete.DefinitionId, cancellationToken);
        await _notificationSender.SendAsync(new WorkflowDefinitionVersionDeleted(definitionToDelete), cancellationToken);

        return isDeleted;
    }

    private async Task EnsureLastVersionIsLatestAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken)
    {
        foreach (var definitionId in definitionIds) 
            await EnsureLastVersionIsLatestAsync(definitionId, cancellationToken);
    }

    /// <summary>
    /// Ensures that the last version of a workflow definition is marked as "IsLatest = true".
    /// </summary>
    /// <param name="definitionId">The definition ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task EnsureLastVersionIsLatestAsync(string definitionId, CancellationToken cancellationToken)
    {
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId };
        var lastVersion = await _store.FindLastVersionAsync(filter, cancellationToken);

        if (lastVersion is null)
            return;

        lastVersion.IsLatest = true;
        await _store.SaveAsync(lastVersion, cancellationToken);
    }
}