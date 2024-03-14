using Elsa.DataSets.Entities;
using Elsa.DataSets.Filters;

namespace Elsa.DataSets.Contracts;

/// <summary>
/// Provides methods to find <see cref="DataSetDefinition"/>s;
/// </summary>
public interface IDataSetDefinitionProvider
{
    /// <summary>
    /// Lists all <see cref="DataSetDefinition"/>s.
    /// </summary>
    ValueTask<IEnumerable<DataSetDefinition>> ListAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds <see cref="DataSetDefinition"/>s that match the given filter.
    /// </summary>
    ValueTask<IEnumerable<DataSetDefinition>> FindManyAsync(DataSetDefinitionFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds a <see cref="DataSetDefinition"/> that matches the given name.
    /// </summary>
    ValueTask<DataSetDefinition?> FindAsync(string name, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds a <see cref="DataSetDefinition"/> that matches the given filter.
    /// </summary>
    ValueTask<DataSetDefinition?> FindAsync(DataSetDefinitionFilter filter, CancellationToken cancellationToken = default);
}