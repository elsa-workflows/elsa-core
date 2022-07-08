using Elsa.CustomActivities.Entities;
using Elsa.CustomActivities.Models;
using Elsa.CustomActivities.Services;
using Elsa.Persistence.Common.Models;
using Elsa.Persistence.EntityFrameworkCore.Common.Extensions;
using Elsa.Persistence.EntityFrameworkCore.Common.Services;

namespace Elsa.CustomActivities.EntityFrameworkCore.Implementations;

public class EFCoreActivityDefinitionStore : IActivityDefinitionStore
{
    private readonly IStore<CustomActivitiesDbContext, ActivityDefinition> _store;

    public EFCoreActivityDefinitionStore(IStore<CustomActivitiesDbContext, ActivityDefinition> store)
    {
        _store = store;
    }

    public async Task SaveAsync(ActivityDefinition record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, cancellationToken);
    public async Task SaveManyAsync(IEnumerable<ActivityDefinition> records, CancellationToken cancellationToken = default) => await _store.SaveManyAsync(records, cancellationToken);

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => await _store.DeleteWhereAsync(x => x.Id == id, cancellationToken) > 0;

    public async Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await _store.DeleteWhereAsync(x => idList.Contains(x.Id), cancellationToken);
    }

    public async Task<ActivityDefinition?> FindByIdAsync(string id, CancellationToken cancellationToken = default) => await _store.FindAsync(x => x.Id == id, cancellationToken);

    public async Task<Page<ActivityDefinition>> ListAsync(PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var set = dbContext.ActivityDefinitions.OrderBy(x => x.Name);
        return await set.PaginateAsync(pageArgs);
    }

    public async Task<IEnumerable<ActivityDefinition>> FindManyByIdAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await _store.FindManyAsync(x => idList.Contains(x.Id), cancellationToken);
    }

    public async Task<Page<ActivityDefinitionSummary>> ListSummariesAsync(VersionOptions? versionOptions = default, string? materializerName = default, PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var workflowDefinitions = dbContext.ActivityDefinitions;
        var query = workflowDefinitions.AsQueryable();

        if (versionOptions != null) query = query.WithVersion(versionOptions.Value);
        if (!string.IsNullOrWhiteSpace(materializerName)) query = query.Where(x => x.MaterializerName == materializerName);

        query = query.OrderBy(x => x.Name); //.Distinct();

        return await query.PaginateAsync(x => WorkflowDefinitionSummary.FromDefinition(x), pageArgs);
    }
}