using System.Linq.Expressions;
using Elsa.CustomActivities.Entities;
using Elsa.CustomActivities.Models;
using Elsa.CustomActivities.Services;
using Elsa.Persistence.Common.Extensions;
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

    public async Task<IEnumerable<ActivityDefinition>> FindLatestAndPublishedByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default) =>
        await _store.FindManyAsync(x => x.DefinitionId == definitionId && (x.IsLatest || x.IsPublished), cancellationToken);

    public async Task SaveAsync(ActivityDefinition record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, cancellationToken);

    public async Task<Page<ActivityDefinitionSummary>> ListSummariesAsync(VersionOptions? versionOptions = default, PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var workflowDefinitions = dbContext.ActivityDefinitions;
        var query = workflowDefinitions.AsQueryable();

        if (versionOptions != null) query = query.WithVersion(versionOptions.Value);

        query = query.OrderBy(x => x.Name);

        return await query.PaginateAsync(x => ActivityDefinitionSummary.FromDefinition(x), pageArgs);
    }

    public async Task<ActivityDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        Expression<Func<ActivityDefinition, bool>> predicate = x => x.DefinitionId == definitionId;
        predicate = predicate.WithVersion(versionOptions);
        return await _store.FindAsync(predicate, cancellationToken);
    }
}