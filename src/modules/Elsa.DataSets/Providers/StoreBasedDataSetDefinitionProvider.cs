using Elsa.DataSets.Contracts;
using Elsa.DataSets.Entities;
using Elsa.DataSets.Filters;

namespace Elsa.DataSets.Providers;

/// <summary>
/// Provides methods to retrieve <see cref="DataSetDefinition"/>s from a data store.
/// </summary>
public class StoreBasedDataSetDefinitionProvider(IDataSetDefinitionStore store) : IDataSetDefinitionProvider
{
    /// <inheritdoc />
    public async ValueTask<IEnumerable<DataSetDefinition>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return await store.FindManyAsync(new DataSetDefinitionFilter(), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<DataSetDefinition>> FindManyAsync(DataSetDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.FindManyAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<DataSetDefinition?> FindAsync(string name, CancellationToken cancellationToken = default)
    {
        var filter = new DataSetDefinitionFilter
        {
            Name = name
        };
        return await store.FindAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<DataSetDefinition?> FindAsync(DataSetDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.FindAsync(filter, cancellationToken);
    }
}