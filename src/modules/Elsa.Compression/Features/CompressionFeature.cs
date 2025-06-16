using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.IO.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Compression.Features;

/// <summary>
/// Configures compression activities and services.
/// </summary>
[UsedImplicitly]
[DependsOn(typeof(IOFeature))]
public class CompressionFeature(IModule module) : FeatureBase(module)
{
    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddHttpClient();
    }
}