using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.DataSets.Contracts;
using Elsa.DataSets.Entities;
using Elsa.DataSets.Filters;
using Elsa.Extensions;

namespace Elsa.DataSets.Stores;

/// <summary>
/// In-memory-based store for <see cref="LinkedServiceDefinition"/>.
/// </summary>
public class MemoryLinkedServiceDefinitionStore(MemoryStore<LinkedServiceDefinition> store) : ILinkedServiceDefinitionStore
{
    /// <inheritdoc />
    public Task<LinkedServiceDefinition?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = store.Find(x => x.Id == id);
        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    public Task<LinkedServiceDefinition?> FindAsync(LinkedServiceDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var result = store.Query(query => Filter(query, filter)).FirstOrDefault();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IEnumerable<LinkedServiceDefinition>> FindManyAsync(LinkedServiceDefinitionFilter definitionFilter, CancellationToken cancellationToken = default)
    {
        var result = store.Query(query => Filter(query, definitionFilter)).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<Page<LinkedServiceDefinition>> FindManyAsync(LinkedServiceDefinitionFilter definitionFilter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = store.Query(query => Filter(query, definitionFilter)).LongCount();
        var result = store.Query(query => Filter(query, definitionFilter).Paginate(pageArgs)).ToList();
        return Task.FromResult(Page.Of(result, count));
    }

    /// <inheritdoc />
    public Task SaveAsync(LinkedServiceDefinition definition, CancellationToken cancellationToken = default)
    {
        store.Save(definition, GetId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<long> DeleteAsync(LinkedServiceDefinitionFilter definitionFilter, CancellationToken cancellationToken = default)
    {
        var ids = store.Query(query => Filter(query, definitionFilter)).Select(x => x.Id).Distinct().ToList();
        store.DeleteWhere(x => ids.Contains(x.Id));
        return Task.FromResult(ids.LongCount());
    }
    
    private IQueryable<LinkedServiceDefinition> Filter(IQueryable<LinkedServiceDefinition> queryable, LinkedServiceDefinitionFilter filter) => filter.Apply(queryable);
    private string GetId(LinkedServiceDefinition dataSetDefinition) => dataSetDefinition.Id;
}