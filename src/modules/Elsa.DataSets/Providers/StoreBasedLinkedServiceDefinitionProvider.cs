using Elsa.DataSets.Contracts;
using Elsa.DataSets.Entities;
using Elsa.DataSets.Filters;

namespace Elsa.DataSets.Providers;

/// <summary>
/// Provides methods to store <see cref="LinkedServiceDefinition"/>s using a <see cref="ILinkedServiceDefinitionStore"/>.
/// </summary>
public class StoreBasedLinkedServiceDefinitionProvider(ILinkedServiceDefinitionStore store) : ILinkedServiceDefinitionProvider
{
    /// <inheritdoc />
    public async ValueTask<IEnumerable<LinkedServiceDefinition>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return await store.FindManyAsync(new LinkedServiceDefinitionFilter(), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<LinkedServiceDefinition>> FindManyAsync(LinkedServiceDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.FindManyAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<LinkedServiceDefinition?> FindAsync(string name, CancellationToken cancellationToken = default)
    {
        var filter = new LinkedServiceDefinitionFilter
        {
            Name = name
        };
        return await store.FindAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<LinkedServiceDefinition?> FindAsync(LinkedServiceDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.FindAsync(filter, cancellationToken);
    }
}