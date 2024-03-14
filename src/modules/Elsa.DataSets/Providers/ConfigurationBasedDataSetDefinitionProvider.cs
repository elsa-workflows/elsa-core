using System.Diagnostics.CodeAnalysis;
using Elsa.DataSets.Contracts;
using Elsa.DataSets.Entities;
using Elsa.DataSets.Filters;
using Elsa.DataSets.Options;
using Microsoft.Extensions.Options;

namespace Elsa.DataSets.Providers;

/// <summary>
/// Provides <see cref="DataSetDefinition"/>s based on the configuration.
/// </summary>
public class ConfigurationBasedDataSetDefinitionProvider(IOptions<DataSetOptions> options) : IDataSetDefinitionProvider
{
    /// <inheritdoc />
    public ValueTask<IEnumerable<DataSetDefinition>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return new(options.Value.DataSetDefinitions);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The filter may contain references to types that cannot be statically analyzed.")]
    public ValueTask<IEnumerable<DataSetDefinition>> FindManyAsync(DataSetDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return new(filter.Apply(options.Value.DataSetDefinitions.AsQueryable()).ToArray());
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The filter may contain references to types that cannot be statically analyzed.")]
    public ValueTask<DataSetDefinition?> FindAsync(DataSetDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return new(filter.Apply(options.Value.DataSetDefinitions.AsQueryable()).FirstOrDefault());
    }
}