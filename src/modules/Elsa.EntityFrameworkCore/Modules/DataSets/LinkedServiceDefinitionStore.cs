using Elsa.Common.Models;
using Elsa.DataSets.Contracts;
using Elsa.DataSets.Entities;
using Elsa.DataSets.Filters;
using Elsa.EntityFrameworkCore.Common;
using Microsoft.EntityFrameworkCore;
using Open.Linq.AsyncExtensions;

namespace Elsa.EntityFrameworkCore.Modules.DataSets;

/// <summary>
/// Represents a store for managing linked service definitions using EF Core.
/// </summary>
public class EFCoreLinkedServiceDefinitionStore(EntityStore<DataSetElsaDbContext, LinkedServiceDefinition> store) : ILinkedServiceDefinitionStore
{
    /// <inheritdoc />
    public async Task<LinkedServiceDefinition?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = new LinkedServiceDefinitionFilter
        {
            Id = id
        };
        return await FindAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<LinkedServiceDefinition?> FindAsync(LinkedServiceDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.QueryAsync(queryable => Filter(queryable, filter), cancellationToken).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LinkedServiceDefinition>> FindManyAsync(LinkedServiceDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.QueryAsync(queryable => Filter(queryable, filter), cancellationToken).ToList();
    }

    /// <inheritdoc />
    public async Task<Page<LinkedServiceDefinition>> FindManyAsync(LinkedServiceDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = await store.QueryAsync(queryable => Filter(queryable, filter), cancellationToken).LongCount();
        var results = await store.QueryAsync(queryable => Paginate(Filter(queryable, filter), pageArgs), cancellationToken).ToList();
        return new(results, count);
    }

    /// <inheritdoc />
    public async Task SaveAsync(LinkedServiceDefinition definition, CancellationToken cancellationToken = default)
    {
        await store.SaveAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> DeleteAsync(LinkedServiceDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await store.CreateDbContextAsync(cancellationToken);
        var set = dbContext.LinkedServiceDefinitions;
        var queryable = set.AsQueryable();
        var ids = await Filter(queryable, filter).Select(x => x.Id).Distinct().ToListAsync(cancellationToken);
        return await store.DeleteWhereAsync(x => ids.Contains(x.Id), cancellationToken);
    }
    
    private IQueryable<LinkedServiceDefinition> Filter(IQueryable<LinkedServiceDefinition> queryable, LinkedServiceDefinitionFilter filter)
    {
        if (filter.Id != null) queryable = queryable.Where(x => x.Id == filter.Id);
        if (filter.Ids != null) queryable = queryable.Where(x => filter.Ids.Contains(x.Id));
        if (filter.Name != null) queryable = queryable.Where(x => x.Name == filter.Name);
        if (filter.Names != null) queryable = queryable.Where(x => filter.Names.Contains(x.Id));

        return queryable;
    }

    private IQueryable<LinkedServiceDefinition> Paginate(IQueryable<LinkedServiceDefinition> queryable, PageArgs? pageArgs)
    {
        if (pageArgs?.Offset != null) queryable = queryable.Skip(pageArgs.Offset.Value);
        if (pageArgs?.Limit != null) queryable = queryable.Take(pageArgs.Limit.Value);
        return queryable;
    }
}