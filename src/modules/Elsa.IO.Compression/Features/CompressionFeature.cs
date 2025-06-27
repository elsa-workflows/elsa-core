using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.IO.Compression.Models;
using Elsa.IO.Compression.Services.Strategies;
using Elsa.IO.Features;
using Elsa.IO.Services.Strategies;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

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
        Module.AddVariableTypeAndAlias<ZipEntry>("ZipEntry", "Compression");
        Module.UseIOHttp();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddScoped<IContentResolverStrategy, ZipEntryContentStrategy>();
    }
}