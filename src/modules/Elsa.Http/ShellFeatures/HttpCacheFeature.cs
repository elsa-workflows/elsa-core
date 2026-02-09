using CShells.Features;
using Elsa.Http.Handlers;
using Elsa.Http.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.ShellFeatures;

/// <summary>
/// Installs services related to HTTP caching.
/// </summary>
[ShellFeature(
    DisplayName = "HTTP Caching",
    Description = "Provides caching capabilities for HTTP workflows",
    DependsOn = ["Http", "CachingWorkflowDefinitions"])]
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
