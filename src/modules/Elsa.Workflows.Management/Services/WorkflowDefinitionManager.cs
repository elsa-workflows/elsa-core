using Elsa.Common.Models;
using Elsa.Mediator.Contracts;
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

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowDefinitionManager(
        IWorkflowDefinitionStore store,
        INotificationSender notificationSender,
        IWorkflowDefinitionPublisher workflowPublisher,
        IIdentityGenerator identityGenerator)
    {
        _store = store;
        _notificationSender = notificationSender;
        _workflowPublisher = workflowPublisher;
        _identityGenerator = identityGenerator;
    }

    /// <inheritdoc />
    public async Task<long> DeleteByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        await _notificationSender.SendAsync(new WorkflowDefinitionDeleting(definitionId), cancellationToken);
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId };
        var count = await _store.DeleteAsync(filter, cancellationToken);
        await _notificationSender.SendAsync(new WorkflowDefinitionDeleted(definitionId), cancellationToken);
        return count;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter { Id = id };
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
        var filter = new WorkflowDefinitionFilter { DefinitionIds = definitionIdList, IsReadonly = false };
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
    public async Task<bool> DeleteVersionAsync(WorkflowDefinition definitionToDelete, CancellationToken cancellationToken)
    {
        if (definitionToDelete.IsPublished)
        {
            await _workflowPublisher.RetractAsync(definitionToDelete, cancellationToken);
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

    /// <inheritdoc />
    public async Task<WorkflowDefinition> RevertVersionAsync(string definitionId, int version, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = VersionOptions.Latest };
        var latestVersion = await _store.FindAsync(filter, cancellationToken);

        if (latestVersion != null)
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