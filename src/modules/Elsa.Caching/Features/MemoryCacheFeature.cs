using Elsa.Caching.Options;
using Elsa.Caching.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Caching.Features;

/// <summary>
/// Configures the MemoryCache.
/// </summary>
public class MemoryCacheFeature(IModule module) : FeatureBase(module)
{
    /// <summary>
    /// A delegate to configure the <see cref="CachingOptions"/>.
    /// </summary>
    public Action<CachingOptions> CachingOptions { get; set; } = _ => { };

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(CachingOptions);
        Services.AddMemoryCache();
        Services.AddSingleton<ICacheManager, CacheManager>();
        Services.AddSingleton<IChangeTokenSignaler, ChangeTokenSignaler>();
    }
}