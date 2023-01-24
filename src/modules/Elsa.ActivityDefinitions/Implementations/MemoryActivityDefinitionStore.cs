using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Models;
using Elsa.ActivityDefinitions.Services;
using Elsa.Common.Implementations;
using Elsa.Common.Models;
using Elsa.Extensions;

namespace Elsa.ActivityDefinitions.Implementations;

public class MemoryActivityDefinitionStore : IActivityDefinitionStore
{
    private readonly MemoryStore<ActivityDefinition> _store;

    public MemoryActivityDefinitionStore(MemoryStore<ActivityDefinition> store)
    {
        _store = store;
    }

    public Task<Page<ActivityDefinition>> ListAsync(VersionOptions? versionOptions = default, PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        var query = _store.List().AsQueryable();
        if (versionOptions != null) query = query.WithVersion(versionOptions.Value);
        var page = query.Paginate(pageArgs);
        return Task.FromResult(page);
    }

    public Task<Page<ActivityDefinitionSummary>> ListSummariesAsync(VersionOptions? versionOptions = default, PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        var query = _store.List().AsQueryable();
        if (versionOptions != null) query = query.WithVersion(versionOptions.Value);
        var page = query.Paginate(x => ActivityDefinitionSummary.FromDefinition(x), pageArgs);
        return Task.FromResult(page);
    }

    public Task<ActivityDefinition?> FindByTypeAsync(string type, int version, CancellationToken cancellationToken = default)
    {
        var definition = _store.Find(x => x.Type == type && x.Version == version);
        return Task.FromResult(definition);
    }

    public Task<ActivityDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var definition = _store.Find(x => x.DefinitionId == definitionId && x.WithVersion(versionOptions));
        return Task.FromResult(definition);
    }
    
    public Task<IEnumerable<ActivityDefinition>> FindManyByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var definition = _store.FindMany(x => x.DefinitionId == definitionId && x.WithVersion(versionOptions));
        return Task.FromResult(definition);
    }
    
    public Task<ActivityDefinition?> FindLastVersionByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken)
    {
        var query = _store.List();
        return Task.FromResult(query.Where(w => w.DefinitionId == definitionId).MaxBy(w => w.Version));
    }

    public Task<IEnumerable<ActivityDefinition>> FindLatestAndPublishedByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var definitions = _store.FindMany(x => x.DefinitionId == definitionId && (x.IsLatest || x.IsPublished));
        return Task.FromResult(definitions);
    }

    public Task SaveAsync(ActivityDefinition record, CancellationToken cancellationToken = default)
    {
        _store.Save(record, x => x.Id);
        return Task.CompletedTask;
    }

    public Task<int> DeleteByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var result = _store.DeleteWhere(x => x.DefinitionId == definitionId);
        return Task.FromResult(result);
    }

    public Task<int> DeleteByDefinitionIdsAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default)
    {
        var definitionIdList = definitionIds.ToList();
        var result = _store.DeleteWhere(x => definitionIdList.Contains(x.DefinitionId));
        return Task.FromResult(result);
    }
    
    public Task<int> DeleteByDefinitionIdAndVersionAsync(string definitionId, int version, CancellationToken cancellationToken = default)
    {
        var result = _store.DeleteWhere(x => x.DefinitionId == definitionId && x.Version == version);
        return Task.FromResult(result);
    }
}