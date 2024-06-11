using Elsa.Common.Models;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Notifications;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowDefinitionManager(
    IWorkflowDefinitionStore store,
    INotificationSender notificationSender,
    IWorkflowDefinitionPublisher workflowPublisher,
    IIdentityGenerator identityGenerator)
    : IWorkflowDefinitionManager
{
    /// <inheritdoc />
    public async Task<long> DeleteByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        await notificationSender.SendAsync(new WorkflowDefinitionDeleting(definitionId), cancellationToken);
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId };
        var count = await store.DeleteAsync(filter, cancellationToken);
        await notificationSender.SendAsync(new WorkflowDefinitionDeleted(definitionId), cancellationToken);
        return count;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter { Id = id };
        var definition = await store.FindAsync(filter, cancellationToken);

        if (definition == null)
            return false;

        return await DeleteVersionAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> BulkDeleteByDefinitionIdsAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default)
    {
        var definitionIdList = definitionIds.Distinct().ToList();
        await notificationSender.SendAsync(new WorkflowDefinitionsDeleting(definitionIdList), cancellationToken);
        var filter = new WorkflowDefinitionFilter { DefinitionIds = definitionIdList, IsReadonly = false };
        var count = await store.DeleteAsync(filter, cancellationToken);
        await EnsureLastVersionIsLatestAsync(definitionIdList, cancellationToken);
        await notificationSender.SendAsync(new WorkflowDefinitionsDeleted(definitionIdList), cancellationToken);
        return count;
    }

    /// <inheritdoc />
    public async Task<long> BulkDeleteByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        var definitions = await store.FindSummariesAsync(new WorkflowDefinitionFilter { Ids = idList }, cancellationToken);
        var definitionIds = definitions.Select(x => x.DefinitionId).Distinct().ToList();
        await notificationSender.SendAsync(new WorkflowDefinitionVersionsDeleting(idList), cancellationToken);
        var filter = new WorkflowDefinitionFilter { Ids = idList };
        var count = await store.DeleteAsync(filter, cancellationToken);
        await EnsureLastVersionIsLatestAsync(definitionIds, cancellationToken);
        await notificationSender.SendAsync(new WorkflowDefinitionVersionsDeleted(idList), cancellationToken);
        return count;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteVersionAsync(string definitionId, int versionToDelete, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = VersionOptions.SpecificVersion(versionToDelete) };
        var definitionToDelete = await store.FindAsync(filter, cancellationToken);

        if (definitionToDelete == null)
            return false;

        return await DeleteVersionAsync(definitionToDelete, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteVersionAsync(WorkflowDefinition definitionToDelete, CancellationToken cancellationToken)
    {
        if (definitionToDelete.IsPublished)
        {
            throw new Exception("Published version cannot be deleted before retracting it");
        }

        await notificationSender.SendAsync(new WorkflowDefinitionVersionDeleting(definitionToDelete), cancellationToken);

        var filter = new WorkflowDefinitionFilter { Id = definitionToDelete.Id };
        var isDeleted = await store.DeleteAsync(filter, cancellationToken) > 0;

        if (!isDeleted)
            return false;

        await EnsureLastVersionIsLatestAsync(definitionToDelete.DefinitionId, cancellationToken);
        await notificationSender.SendAsync(new WorkflowDefinitionVersionDeleted(definitionToDelete), cancellationToken);

        return isDeleted;
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition> RevertVersionAsync(string definitionId, int version, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = VersionOptions.Latest };
        var latestVersion = await store.FindAsync(filter, cancellationToken);

        if (latestVersion != null)
        {
            latestVersion.IsLatest = false;
            await store.SaveAsync(latestVersion, cancellationToken);
        }

        var draft = await workflowPublisher.GetDraftAsync(definitionId, VersionOptions.SpecificVersion(version), cancellationToken);
        draft!.Id = identityGenerator.GenerateId();
        draft.Version = (latestVersion?.Version ?? 0) + 1;
        draft.IsLatest = true;

        await store.SaveAsync(draft, cancellationToken);
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
        var lastVersion = await store.FindLastVersionAsync(filter, cancellationToken);

        if (lastVersion is null)
            return;

        lastVersion.IsLatest = true;
        await store.SaveAsync(lastVersion, cancellationToken);
    }
}