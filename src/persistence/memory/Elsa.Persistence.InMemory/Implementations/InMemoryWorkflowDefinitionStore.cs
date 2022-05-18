using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Extensions;
using Elsa.Persistence.Models;
using Elsa.Persistence.Services;

namespace Elsa.Persistence.InMemory.Implementations;

public class InMemoryWorkflowDefinitionStore : IWorkflowDefinitionStore
{
    private readonly InMemoryStore<WorkflowDefinition> _store;
    private readonly InMemoryStore<WorkflowInstance> _instanceStore;
    private readonly InMemoryStore<WorkflowTrigger> _triggerStore;
    private readonly InMemoryStore<WorkflowBookmark> _bookmarkStore;

    public InMemoryWorkflowDefinitionStore(
        InMemoryStore<WorkflowDefinition> store, 
        InMemoryStore<WorkflowInstance> instanceStore, 
        InMemoryStore<WorkflowTrigger> triggerStore, 
        InMemoryStore<WorkflowBookmark> bookmarkStore)
    {
        _store = store;
        _instanceStore = instanceStore;
        _triggerStore = triggerStore;
        _bookmarkStore = bookmarkStore;
    }

    public Task<WorkflowDefinition?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var definition = _store.Find(x => x.Id == id);
        return Task.FromResult(definition);
    }

    public Task<WorkflowDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var definition = _store.Find(x => x.DefinitionId == definitionId && x.WithVersion(versionOptions));
        return Task.FromResult(definition);
    }

    public Task<WorkflowDefinition?> FindByNameAsync(string name, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var definition = _store.Find(x => x.Name == name && x.WithVersion(versionOptions));
        return Task.FromResult(definition);
    }

    public Task<IEnumerable<WorkflowDefinitionSummary>> FindManySummariesAsync(IEnumerable<string> definitionIds, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default)
    {
        var query = _store.List();

        if (versionOptions != null)
            query = query.WithVersion(versionOptions.Value);

        var definitionIdList = definitionIds.ToList();
        query = query.Where(x => definitionIdList.Contains(x.Id));
        
        var summaries = query.Select(WorkflowDefinitionSummary.FromDefinition).ToList().AsEnumerable();
        return Task.FromResult(summaries);
    }

    public Task<IEnumerable<WorkflowDefinition>> FindLatestAndPublishedByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var definitions = _store.FindMany(x => x.DefinitionId == definitionId && (x.IsLatest || x.IsPublished));
        return Task.FromResult(definitions);
    }

    public Task SaveAsync(WorkflowDefinition record, CancellationToken cancellationToken = default)
    {
        _store.Save(record);
        return Task.CompletedTask;
    }

    public Task SaveManyAsync(IEnumerable<WorkflowDefinition> records, CancellationToken cancellationToken = default)
    {
        _store.SaveMany(records);
        return Task.CompletedTask;
    }

    public Task<int> DeleteByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        _triggerStore.DeleteWhere(x => x.WorkflowDefinitionId == definitionId);
        _instanceStore.DeleteWhere(x => x.DefinitionId == definitionId);
        _bookmarkStore.DeleteWhere(x => x.WorkflowDefinitionId == definitionId);
        var result = _store.DeleteWhere(x => x.DefinitionId == definitionId);
        return Task.FromResult(result);
    }

    public Task<int> DeleteManyByDefinitionIdsAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default)
    {
        var definitionIdList = definitionIds.ToList();
        _triggerStore.DeleteWhere(x => definitionIdList.Contains(x.WorkflowDefinitionId));
        _instanceStore.DeleteWhere(x => definitionIdList.Contains(x.DefinitionId));
        _bookmarkStore.DeleteWhere(x => definitionIdList.Contains(x.WorkflowDefinitionId));
        var result = _store.DeleteWhere(x => definitionIdList.Contains(x.DefinitionId));
        return Task.FromResult(result);
    }

    public Task<Page<WorkflowDefinitionSummary>> ListSummariesAsync(VersionOptions? versionOptions = default, string? materializerName = default, PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        var query = _store.List().AsQueryable();

        if (versionOptions != null)
            query = query.WithVersion(versionOptions.Value);
        
        if (!string.IsNullOrWhiteSpace(materializerName))
            query = query.Where(x => x.MaterializerName == materializerName);

        return query.PaginateAsync(x => WorkflowDefinitionSummary.FromDefinition(x), pageArgs);
    }

    public Task<bool> GetExistsAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var exists = _store.AnyAsync(x => x.DefinitionId == definitionId && x.WithVersion(versionOptions));
        return Task.FromResult(exists);
    }
}