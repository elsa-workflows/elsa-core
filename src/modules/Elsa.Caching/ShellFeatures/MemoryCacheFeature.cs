using CShells.Features;
using Elsa.Caching.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Caching.ShellFeatures;

/// <summary>
/// Configures the MemoryCache.
/// </summary>
[ShellFeature(
    DisplayName = "Memory Cache",
    Description = "Provides in-memory caching services")]
[UsedImplicitly]
public class MemoryCacheFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddMemoryCache()
            .AddSingleton<ICacheManager, CacheManager>()
            .AddSingleton<IChangeTokenSignalInvoker, ChangeTokenSignalInvoker>()
            .AddSingleton<IChangeTokenSignaler, ChangeTokenSignaler>();
    }
}
