using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.IO.Features;
using JetBrains.Annotations;

namespace Elsa.IO.Compression.Features;

/// <summary>
/// Configures compression activities and services.
/// </summary>
[UsedImplicitly]
[DependsOn(typeof(IOFeature))]
public class CompressionFeature(IModule module) : FeatureBase(module)
{
    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddActivitiesFrom<CompressionFeature>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        // Compression-specific services can be added here when needed
    }
}