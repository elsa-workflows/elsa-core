using Elsa.DataSets.Entities;
using Elsa.DataSets.Filters;

namespace Elsa.DataSets.Contracts;

/// <summary>
/// Provides methods to find <see cref="LinkedServiceDefinition"/>s;
/// </summary>
public interface ILinkedServiceDefinitionProvider
{
    /// <summary>
    /// Lists all <see cref="LinkedServiceDefinition"/>s.
    /// </summary>
    ValueTask<IEnumerable<LinkedServiceDefinition>> ListAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds <see cref="LinkedServiceDefinition"/>s that match the given filter.
    /// </summary>
    ValueTask<IEnumerable<LinkedServiceDefinition>> FindManyAsync(LinkedServiceDefinitionFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds a <see cref="LinkedServiceDefinition"/> that matches the given filter.
    /// </summary>
    ValueTask<LinkedServiceDefinition?> FindAsync(LinkedServiceDefinitionFilter filter, CancellationToken cancellationToken = default);
}