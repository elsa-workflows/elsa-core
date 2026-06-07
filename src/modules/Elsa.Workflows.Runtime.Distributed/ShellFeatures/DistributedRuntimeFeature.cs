using CShells.Features;
using Elsa.Common;
using Elsa.Extensions;
using Elsa.Resilience.ShellFeatures;
using Elsa.Workflows.Runtime.Distributed.StartupTasks;
using Elsa.Workflows.Runtime.ShellFeatures;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Distributed.ShellFeatures;

/// <summary>
/// Installs and configures distributed workflow runtime features.
/// </summary>
[ManifestFeatureCategory(ManifestFeatureCategories.Workflows)]
[ManifestFeatureCategory(ManifestFeatureCategories.Infrastructure)]
[ShellFeature(
    DisplayName = "Distributed Runtime",
    Description = "Provides distributed workflow runtime capabilities",
    DependsOn = [typeof(WorkflowRuntimeFeature), typeof(ResilienceFeature)])]
[UsedImplicitly]
public class DistributedRuntimeFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddScoped<DistributedWorkflowRuntime>()
            .AddScoped<IWorkflowRuntime>(sp => sp.GetRequiredService<DistributedWorkflowRuntime>())
            .AddScoped<DistributedBookmarkQueueWorker>()
            .AddScoped<IBookmarkQueueWorker>(sp => sp.GetRequiredService<DistributedBookmarkQueueWorker>());

        services.TryAddScoped<DistributedRuntimeLockProviderValidator>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IStartupTask, ValidateDistributedRuntimeLockProviderStartupTask>());
    }
}
