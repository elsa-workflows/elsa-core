using Elsa.IO.Compression.Features;
using Elsa.Features.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions to install the <see cref="CompressionFeature"/> feature.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Install the <see cref="CompressionFeature"/> feature.
    /// </summary>
    public static IModule UseCompression(this IModule module, Action<CompressionFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}