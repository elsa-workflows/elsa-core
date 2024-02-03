using Elsa.Workflows.Management.Compression;
using Elsa.Workflows.Management.Contracts;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class CompressionStrategyResolver(IEnumerable<ICompressionStrategy> strategies) : ICompressionStrategyResolver
{
    /// <inheritdoc />
    public ICompressionStrategy Resolve(string name)
    {
        return strategies.FirstOrDefault(s => s.GetType().Name == name) ?? new None();
    }
}