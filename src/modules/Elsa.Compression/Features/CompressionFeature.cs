using Elsa.Compression.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Compression.Features;

/// <summary>
/// Configures compression activities and services.
/// </summary>
[UsedImplicitly]
public class CompressionFeature(IModule module) : FeatureBase(module)
{
    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .AddHttpClient()
            .AddSingleton<IZipEntryContentResolver, ZipEntryContentResolver>();
    }
}