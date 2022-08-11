using System.Linq.Expressions;
using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Models;
using Elsa.ActivityDefinitions.Services;
using Elsa.Persistence.Common.Extensions;
using Elsa.Persistence.Common.Models;
using Elsa.Persistence.EntityFrameworkCore.Common.Extensions;
using Elsa.Persistence.EntityFrameworkCore.Common.Services;

namespace Elsa.ActivityDefinitions.EntityFrameworkCore.Implementations;

public class EFCoreActivityDefinitionStore : IActivityDefinitionStore
{
    private readonly IStore<ActivityDefinitionsDbContext, ActivityDefinition> _store;

    public EFCoreActivityDefinitionStore(IStore<ActivityDefinitionsDbContext, ActivityDefinition> store)
    {
        _store = store;
    }

    public async Task<IEnumerable<ActivityDefinition>> FindLatestAndPublishedByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default) =>
        await _store.FindManyAsync(x => x.DefinitionId == definitionId && (x.IsLatest || x.IsPublished), cancellationToken);

    public async Task SaveAsync(ActivityDefinition record, CancellationToken cancellationToken = default) => await _store.SaveAsync(record, cancellationToken);

    public async Task<Page<ActivityDefinition>> ListAsync(VersionOptions? versionOptions = default, PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var workflowDefinitions = dbContext.ActivityDefinitions;
        var query = workflowDefinitions.AsQueryable();

        if (versionOptions != null) query = query.WithVersion(versionOptions.Value);

        query = query.OrderBy(x => x.DisplayName).ThenBy(x => x.Type);

        return await query.PaginateAsync(pageArgs);
    }

    public async Task<Page<ActivityDefinitionSummary>> ListSummariesAsync(VersionOptions? versionOptions = default, PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        var workflowDefinitions = dbContext.ActivityDefinitions;
        var query = workflowDefinitions.AsQueryable();

        if (versionOptions != null) query = query.WithVersion(versionOptions.Value);

        query = query.OrderBy(x => x.DisplayName).ThenBy(x => x.Type);

        return await query.PaginateAsync(x => ActivityDefinitionSummary.FromDefinition(x), pageArgs);
    }

    public async Task<ActivityDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        Expression<Func<ActivityDefinition, bool>> predicate = x => x.DefinitionId == definitionId;
        predicate = predicate.WithVersion(versionOptions);
        return await _store.FindAsync(predicate, cancellationToken);
    }

    public async Task<ActivityDefinition?> FindByDefinitionVersionIdAsync(string definitionVersionId, CancellationToken cancellationToken = default) =>
        await _store.FindAsync(x => x.Id == definitionVersionId, cancellationToken);

    public async Task<int> DeleteByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default) =>
        await _store.DeleteWhereAsync(x => x.DefinitionId == definitionId, cancellationToken);

    public async Task<int> DeleteByDefinitionIdsAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default)
    {
        var definitionIdList = definitionIds.ToList();
        await using var dbContext = await _store.CreateDbContextAsync(cancellationToken);
        return await _store.DeleteWhereAsync(x => definitionIdList.Contains(x.DefinitionId), cancellationToken);
    }
}