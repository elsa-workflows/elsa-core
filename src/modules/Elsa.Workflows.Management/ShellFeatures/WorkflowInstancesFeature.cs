using CShells.Features;
using Elsa.Workflows.Management.Stores;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.ShellFeatures;

/// <summary>
/// Enables storage of workflow instances.
/// </summary>
[ManifestFeatureCategory(ManifestFeatureCategories.Workflows)]
[ShellFeature(
    DisplayName = "Workflow Instances",
    Description = "Manages workflow execution instances and their persistent state")]
[UsedImplicitly]
public class WorkflowInstancesFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IWorkflowInstanceStore, MemoryWorkflowInstanceStore>();
    }
}
