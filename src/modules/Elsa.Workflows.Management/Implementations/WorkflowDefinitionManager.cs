using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Mediator.Services;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Management.Services;

namespace Elsa.Workflows.Management.Implementations;

/// <inheritdoc />
public class WorkflowDefinitionManager : IWorkflowDefinitionManager
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IEventPublisher _eventPublisher;
    private readonly IWorkflowDefinitionPublisher _workflowPublisher;
    private readonly IIdentityGenerator _identityGenerator;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowDefinitionManager(
        IWorkflowDefinitionStore store,
        IEventPublisher eventPublisher,
        IWorkflowDefinitionPublisher workflowPublisher,
        IIdentityGenerator identityGenerator)
    {
        _store = store;
        _eventPublisher = eventPublisher;
        _workflowPublisher = workflowPublisher;
        _identityGenerator = identityGenerator;
    }

    /// <inheritdoc />
    public async Task<int> DeleteByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var count = await _store.DeleteByDefinitionIdAsync(definitionId, cancellationToken);
        await _eventPublisher.PublishAsync(new WorkflowDefinitionDeleted(definitionId), cancellationToken);
        return count;
    }

    /// <inheritdoc />
    public async Task<int> BulkDeleteByDefinitionIdsAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default)
    {
        var ids = definitionIds.ToList();
        var count = await _store.DeleteByDefinitionIdsAsync(ids, cancellationToken);
        await _eventPublisher.PublishAsync(new WorkflowDefinitionsDeleted(ids), cancellationToken);
        return count;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteVersionAsync(string definitionId, int versionToDelete, CancellationToken cancellationToken = default)
    {
        var workflows = (await _store.FindManyByDefinitionIdAsync(definitionId, VersionOptions.LatestAndPublished, cancellationToken)).ToList();
        var latestVersion = workflows.WithVersion(VersionOptions.Latest).First();
        var publishedVersion = workflows.WithVersion(VersionOptions.Published).FirstOrDefault();

        if (versionToDelete == publishedVersion?.Version)
        {
            throw new Exception("Published version cannot be deleted");
        }

        var isDeleted = await _store.DeleteByDefinitionIdAndVersionAsync(definitionId, versionToDelete, cancellationToken) > 0;

        if (!isDeleted)
            return false;

        await _eventPublisher.PublishAsync(new WorkflowDefinitionVersionDeleted(definitionId, versionToDelete), cancellationToken);

        if (latestVersion.Version != versionToDelete)
            return isDeleted;

        var lastVersion = await _store.FindLastVersionAsync(definitionId, cancellationToken);

        if (lastVersion is null)
            return isDeleted;

        lastVersion.IsLatest = true;
        await _store.SaveAsync(lastVersion, cancellationToken);

        return isDeleted;
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition> RevertVersionAsync(string definitionId, int version, CancellationToken cancellationToken = default)
    {
        var publishedAndLatestVersions = (await _store.FindManyByDefinitionIdAsync(definitionId, VersionOptions.LatestAndPublished, cancellationToken)).ToList();

        if (publishedAndLatestVersions.Any(v => v.Version == version))
        {
            throw new Exception("Latest or published versions cannot be reverted");
        }

        var latestVersion = publishedAndLatestVersions.First(v => v.IsLatest);
        latestVersion.IsLatest = false;
        await _store.SaveAsync(latestVersion, cancellationToken);

        var draft = await _workflowPublisher.GetDraftAsync(definitionId, version, cancellationToken);
        draft!.Id = _identityGenerator.GenerateId();
        draft.Version = latestVersion.Version + 1;
        draft.IsLatest = true;

        await _store.SaveAsync(draft, cancellationToken);
        return draft;
    }
}