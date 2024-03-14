using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.DataSets.Contracts;
using Elsa.DataSets.Entities;
using Elsa.DataSets.Filters;
using Elsa.Extensions;

namespace Elsa.DataSets.Stores;

/// <summary>
/// In-memory store for <see cref="DataSetDefinition"/>.
/// </summary>
public class MemoryDataSetDefinitionStore(MemoryStore<DataSetDefinition> store) : IDataSetDefinitionStore
{
    /// <inheritdoc />
    public Task<DataSetDefinition?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = store.Find(x => x.Id == id);
        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    public Task<DataSetDefinition?> FindAsync(DataSetDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var result = store.Query(query => Filter(query, filter)).FirstOrDefault();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IEnumerable<DataSetDefinition>> FindManyAsync(DataSetDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var result = store.Query(query => Filter(query, filter)).ToList().AsEnumerable();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<Page<DataSetDefinition>> FindManyAsync(DataSetDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var count = store.Query(query => Filter(query, filter)).LongCount();
        var result = store.Query(query => Filter(query, filter).Paginate(pageArgs)).ToList();
        return Task.FromResult(Page.Of(result, count));
    }

    /// <inheritdoc />
    public Task SaveAsync(DataSetDefinition definition, CancellationToken cancellationToken = default)
    {
        store.Save(definition, GetId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<long> DeleteAsync(DataSetDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var ids = store.Query(query => Filter(query, filter)).Select(x => x.Id).Distinct().ToList();
        store.DeleteWhere(x => ids.Contains(x.Id));
        return Task.FromResult(ids.LongCount());
    }
    
    private IQueryable<DataSetDefinition> Filter(IQueryable<DataSetDefinition> queryable, DataSetDefinitionFilter filter) => filter.Apply(queryable);
    private string GetId(DataSetDefinition dataSetDefinition) => dataSetDefinition.Id;
}