using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Notifications;

namespace Elsa.Workflows.Management.Services;

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
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId };
        var count = await _store.DeleteAsync(filter, cancellationToken);
        await _eventPublisher.PublishAsync(new WorkflowDefinitionDeleted(definitionId), cancellationToken);
        return count;
    }

    /// <inheritdoc />
    public async Task<int> BulkDeleteByDefinitionIdsAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default)
    {
        var ids = definitionIds.ToList();
        var filter = new WorkflowDefinitionFilter { DefinitionIds = ids };
        var count = await _store.DeleteAsync(filter, cancellationToken);
        await _eventPublisher.PublishAsync(new WorkflowDefinitionsDeleted(ids), cancellationToken);
        return count;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteVersionAsync(string definitionId, int versionToDelete, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = VersionOptions.LatestAndPublished };
        var workflows = (await _store.FindManyAsync(filter, cancellationToken)).ToList();
        var latestVersion = workflows.WithVersion(VersionOptions.Latest).First();
        var publishedVersion = workflows.WithVersion(VersionOptions.Published).FirstOrDefault();

        if (versionToDelete == publishedVersion?.Version)
        {
            throw new Exception("Published version cannot be deleted");
        }

        filter = new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = VersionOptions.SpecificVersion(versionToDelete) };
        var isDeleted = await _store.DeleteAsync(filter, cancellationToken) > 0;

        if (!isDeleted)
            return false;

        await _eventPublisher.PublishAsync(new WorkflowDefinitionVersionDeleted(definitionId, versionToDelete), cancellationToken);

        if (latestVersion.Version != versionToDelete)
            return isDeleted;

        filter = new WorkflowDefinitionFilter { DefinitionId = definitionId };
        var lastVersion = await _store.FindLastVersionAsync(filter, cancellationToken);

        if (lastVersion is null)
            return isDeleted;

        lastVersion.IsLatest = true;
        await _store.SaveAsync(lastVersion, cancellationToken);

        return isDeleted;
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition> RevertVersionAsync(string definitionId, int version, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = VersionOptions.LatestAndPublished };
        var publishedAndLatestVersions = (await _store.FindManyAsync(filter, cancellationToken)).ToList();

        if (publishedAndLatestVersions.Any(v => v.Version == version))
            throw new Exception("Latest or published versions cannot be reverted");

        var latestVersion = publishedAndLatestVersions.First(v => v.IsLatest);
        latestVersion.IsLatest = false;
        await _store.SaveAsync(latestVersion, cancellationToken);

        var draft = await _workflowPublisher.GetDraftAsync(definitionId, VersionOptions.SpecificVersion(version), cancellationToken);
        draft!.Id = _identityGenerator.GenerateId();
        draft.Version = latestVersion.Version + 1;
        draft.IsLatest = true;

        await _store.SaveAsync(draft, cancellationToken);
        return draft;
    }
}