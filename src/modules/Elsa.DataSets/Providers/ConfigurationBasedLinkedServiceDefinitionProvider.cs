using System.Diagnostics.CodeAnalysis;
using Elsa.DataSets.Contracts;
using Elsa.DataSets.Entities;
using Elsa.DataSets.Filters;
using Elsa.DataSets.Options;
using Microsoft.Extensions.Options;

namespace Elsa.DataSets.Providers;

/// <summary>
/// Provides <see cref="LinkedServiceDefinition"/>s based on the configuration.
/// </summary>
public class ConfigurationBasedLinkedServiceDefinitionProvider(IOptions<DataSetOptions> options) : ILinkedServiceDefinitionProvider
{
    /// <inheritdoc />
    public ValueTask<IEnumerable<LinkedServiceDefinition>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return new(options.Value.LinkedServiceDefinitions);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The filter may contain references to types that cannot be statically analyzed.")]
    public ValueTask<IEnumerable<LinkedServiceDefinition>> FindManyAsync(LinkedServiceDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return new(filter.Apply(options.Value.LinkedServiceDefinitions.AsQueryable()).ToArray());
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The filter may contain references to types that cannot be statically analyzed.")]
    public ValueTask<LinkedServiceDefinition?> FindAsync(LinkedServiceDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return new(filter.Apply(options.Value.LinkedServiceDefinitions.AsQueryable()).FirstOrDefault());
    }
}