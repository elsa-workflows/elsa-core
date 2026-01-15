using CShells.Features;
using Elsa.Workflows.Management.Stores;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.ShellFeatures;

/// <summary>
/// Enables storage of workflow instances.
/// </summary>
[ShellFeature(
    DisplayName = "Workflow Instances",
    Description = "Manages workflow execution instances and their persistent state")]
[UsedImplicitly]
public class WorkflowInstancesFeature : IShellFeature
{
    private Func<IServiceProvider, IWorkflowInstanceStore> WorkflowInstanceStore { get; set; } = sp => sp.GetRequiredService<MemoryWorkflowInstanceStore>();

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped(WorkflowInstanceStore);
    }
}
