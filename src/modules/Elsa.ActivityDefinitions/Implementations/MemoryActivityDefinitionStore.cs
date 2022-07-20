using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Models;
using Elsa.ActivityDefinitions.Services;
using Elsa.Persistence.Common.Extensions;
using Elsa.Persistence.Common.Implementations;
using Elsa.Persistence.Common.Models;

namespace Elsa.ActivityDefinitions.Implementations;

public class MemoryActivityDefinitionStore : IActivityDefinitionStore
{
    private readonly MemoryStore<ActivityDefinition> _store;

    public MemoryActivityDefinitionStore(MemoryStore<ActivityDefinition> store)
    {
        _store = store;
    }

    public Task<Page<ActivityDefinitionSummary>> ListSummariesAsync(VersionOptions? versionOptions = default, PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        var query = _store.List().AsQueryable();
        if (versionOptions != null) query = query.WithVersion(versionOptions.Value);
        var page = query.Paginate(x => ActivityDefinitionSummary.FromDefinition(x), pageArgs);
        return Task.FromResult(page);
    }

    public Task<ActivityDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var definition = _store.Find(x => x.DefinitionId == definitionId && x.WithVersion(versionOptions));
        return Task.FromResult(definition);
    }

    public Task<IEnumerable<ActivityDefinition>> FindLatestAndPublishedByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var definitions = _store.FindMany(x => x.DefinitionId == definitionId && (x.IsLatest || x.IsPublished));
        return Task.FromResult(definitions);
    }

    public Task SaveAsync(ActivityDefinition record, CancellationToken cancellationToken = default)
    {
        _store.Save(record);
        return Task.CompletedTask;
    }
}