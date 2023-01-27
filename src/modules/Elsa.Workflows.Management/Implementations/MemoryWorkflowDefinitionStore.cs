using Elsa.Common.Implementations;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;

namespace Elsa.Workflows.Management.Implementations;

/// <summary>
/// A memory implementation of <see cref="IWorkflowDefinitionStore"/>.
/// </summary>
public class MemoryWorkflowDefinitionStore : IWorkflowDefinitionStore
{
    private readonly MemoryStore<WorkflowDefinition> _store;
    private readonly MemoryStore<WorkflowInstance> _instanceStore;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MemoryWorkflowDefinitionStore(
        MemoryStore<WorkflowDefinition> store,
        MemoryStore<WorkflowInstance> instanceStore)
    {
        _store = store;
        _instanceStore = instanceStore;
    }

    /// <inheritdoc />
    public Task<WorkflowDefinition?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var definition = _store.Find(x => x.Id == id);
        return Task.FromResult(definition);
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinition>> FindManyByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var definition = _store.FindMany(x => x.DefinitionId == definitionId && x.WithVersion(versionOptions));
        return Task.FromResult(definition);
    }

    /// <inheritdoc />
    public Task<WorkflowDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var definition = _store.Find(x => x.DefinitionId == definitionId && x.WithVersion(versionOptions));
        return Task.FromResult(definition);
    }

    /// <inheritdoc />
    public Task<WorkflowDefinition?> FindByNameAsync(string name, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var definition = _store.Find(x => x.Name == name && x.WithVersion(versionOptions));
        return Task.FromResult(definition);
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinition>> FindWithActivityBehaviorAsync(VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var definition = _store.FindMany(w => w.UsableAsActivity == true && w.WithVersion(versionOptions));
        return Task.FromResult(definition);
    }

    /// <inheritdoc />
    public Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync(IEnumerable<string> definitionIds, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default)
    {
        var query = _store.List();

        if (versionOptions != null)
            query = query.WithVersion(versionOptions.Value);

        var definitionIdList = definitionIds.ToList();
        query = query.Where(x => definitionIdList.Contains(x.Id));

        var summaries = query.Select(WorkflowDefinitionSummary.FromDefinition).ToList().AsEnumerable();
        return Task.FromResult(summaries);
    }
    
    /// <inheritdoc />
    public Task<WorkflowDefinition?> FindLastVersionAsync(string definitionId, CancellationToken cancellationToken)
    {
        var query = _store.List();
        return Task.FromResult(query.Where(w => w.DefinitionId == definitionId).MaxBy(w => w.Version));
    }

    /// <inheritdoc />
    public Task SaveAsync(WorkflowDefinition record, CancellationToken cancellationToken = default)
    {
        _store.Save(record, GetId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveManyAsync(IEnumerable<WorkflowDefinition> records, CancellationToken cancellationToken = default)
    {
        _store.SaveMany(records, GetId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<int> DeleteByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        _instanceStore.DeleteWhere(x => x.DefinitionId == definitionId);
        var result = _store.DeleteWhere(x => x.DefinitionId == definitionId);
        return Task.FromResult(result);
    }
    
    /// <inheritdoc />
    public Task<int> DeleteByDefinitionIdAndVersionAsync(string definitionId, int version, CancellationToken cancellationToken = default)
    {
        _instanceStore.DeleteWhere(x => x.DefinitionId == definitionId && x.Version == version);
        var result = _store.DeleteWhere(x => x.DefinitionId == definitionId && x.Version == version);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<int> DeleteByDefinitionIdsAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default)
    {
        var definitionIdList = definitionIds.ToList();
        _instanceStore.DeleteWhere(x => definitionIdList.Contains(x.DefinitionId));
        var result = _store.DeleteWhere(x => definitionIdList.Contains(x.DefinitionId));
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<Page<WorkflowDefinitionSummary>> ListSummariesAsync(
        VersionOptions? versionOptions = default,
        string? materializerName = default,
        PageArgs? pageArgs = default,
        CancellationToken cancellationToken = default)
    {
        var query = _store.List().AsQueryable();

        if (versionOptions != null) query = query.WithVersion(versionOptions.Value);
        if (!string.IsNullOrWhiteSpace(materializerName)) query = query.Where(x => x.MaterializerName == materializerName);

        var page = query.Paginate(x => WorkflowDefinitionSummary.FromDefinition(x), pageArgs);
        return Task.FromResult(page);
    }

    /// <inheritdoc />
    public Task<bool> GetExistsAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var exists = _store.AnyAsync(x => x.DefinitionId == definitionId && x.WithVersion(versionOptions));
        return Task.FromResult(exists);
    }

    private string GetId(WorkflowDefinition workflowDefinition) => workflowDefinition.Id;
}