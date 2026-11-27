using CShells.Features;
using Elsa.Caching.ShellFeatures;
using Elsa.Workflows.Runtime.Handlers;
using Elsa.Workflows.Runtime.Stores;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.ShellFeatures;

/// <summary>
/// Installs and configures workflow runtime caching features.
/// </summary>
[ManifestFeatureCategory("Workflows")]
[ManifestFeatureCategory("Caching")]
[ShellFeature(
    DisplayName = "Caching Workflow Runtime",
    Description = "Provides caching for workflow runtime operations",
    DependsOn = [typeof(MemoryCacheFeature), typeof(WorkflowRuntimeFeature)])]
[UsedImplicitly]
public class CachingWorkflowRuntimeFeature : IShellFeature, IPostConfigureShellServices
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddNotificationHandler<InvalidateTriggersCache>()
            .AddNotificationHandler<InvalidateWorkflowsCache>();
    }

    /// <summary>
    /// Caches the trigger store that the enabled features registered, including a persistence feature that registers its store after this
    /// feature, on top of the decorators of <see cref="WorkflowRuntimeFeature"/>.
    /// </summary>
    public void PostConfigureServices(IServiceCollection services)
    {
        services.Decorate<ITriggerStore, CachingTriggerStore>();
    }
}


