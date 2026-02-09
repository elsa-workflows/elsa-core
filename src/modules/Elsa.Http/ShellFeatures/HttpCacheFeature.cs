using CShells.Features;
using Elsa.Http.Handlers;
using Elsa.Http.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.ShellFeatures;

/// <summary>
/// Installs services related to HTTP caching.
/// </summary>
[ShellFeature(
    DisplayName = "HTTP Cache",
    Description = "Provides HTTP workflow caching for improved performance",
    DependsOn = ["Http", "CachingWorkflowDefinitions"])]
[UsedImplicitly]
public class HttpCacheFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSingleton<IHttpWorkflowsCacheManager, HttpWorkflowsCacheManager>()
            .Decorate<IHttpWorkflowLookupService, CachingHttpWorkflowLookupService>()
            .AddNotificationHandler<InvalidateHttpWorkflowsCache>();
    }
}


