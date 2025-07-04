using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.IO.Compression.Features;
using Elsa.IO.Features;
using Elsa.IO.Http.Common;
using Elsa.IO.Http.Services.Strategies;
using Elsa.IO.Services.Strategies;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.IO.Http.Features;

/// <summary>
/// Configures HTTP-based IO services.
/// </summary>
[UsedImplicitly]
[DependsOn(typeof(IOFeature))]
[DependencyOf(typeof(CompressionFeature))]
public class IOHttpFeature(IModule module) : FeatureBase(module)
{
    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddHttpClient(Constants.IOFeatureHttpClient);
        
        Services.AddScoped<IContentResolverStrategy, UrlContentStrategy>();
    }
}
