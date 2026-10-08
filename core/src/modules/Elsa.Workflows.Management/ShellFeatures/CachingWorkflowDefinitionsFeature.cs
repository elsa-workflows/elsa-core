using CShells.Features;
using Elsa.Workflows.Management.Handlers.Notifications;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Management.Stores;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.ShellFeatures;

/// <summary>
/// Configures workflow definition caching.
/// </summary>
[ManifestFeatureCategory("Workflows")]
[ManifestFeatureCategory("Caching")]
[ShellFeature(
    DisplayName = "Caching Workflow Definitions",
    Description = "Provides caching for workflow definitions")]
[UsedImplicitly]
public class CachingWorkflowDefinitionsFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IWorkflowDefinitionCacheManager, WorkflowDefinitionCacheManager>();
        services.Decorate<IWorkflowDefinitionStore, CachingWorkflowDefinitionStore>();
        services.Decorate<IWorkflowDefinitionService, CachingWorkflowDefinitionService>();
        services.AddNotificationHandler<EvictWorkflowDefinitionServiceCache>();
    }
}


