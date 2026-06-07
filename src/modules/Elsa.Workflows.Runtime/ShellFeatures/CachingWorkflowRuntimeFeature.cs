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
[ManifestFeatureCategory(ManifestFeatureCategories.Workflows)]
[ManifestFeatureCategory(ManifestFeatureCategories.Caching)]
[ShellFeature(
    DisplayName = "Caching Workflow Runtime",
    Description = "Provides caching for workflow runtime operations",
    DependsOn = [typeof(MemoryCacheFeature), typeof(WorkflowRuntimeFeature)])]
[UsedImplicitly]
public class CachingWorkflowRuntimeFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            // Decorators.
            .Decorate<ITriggerStore, CachingTriggerStore>()

            // Handlers.
            .AddNotificationHandler<InvalidateTriggersCache>()
            .AddNotificationHandler<InvalidateWorkflowsCache>();
    }
}


