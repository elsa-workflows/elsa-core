using CShells.Features;
using Elsa.Http.Handlers;
using Elsa.Http.Services;
using Elsa.Workflows.Management.ShellFeatures;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.ShellFeatures;

/// <summary>
/// Installs services related to HTTP caching.
/// </summary>
[ManifestFeatureCategory("HTTP")]
[ManifestFeatureCategory("Caching")]
[ShellFeature(
    DisplayName = "HTTP Cache",
    Description = "Provides HTTP workflow caching for improved performance",
    DependsOn = [typeof(HttpFeature), typeof(CachingWorkflowDefinitionsFeature)])]
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


