using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Distributed.ShellFeatures;

/// <summary>
/// Installs and configures distributed workflow runtime features.
/// </summary>
[ShellFeature(
    DisplayName = "Distributed Runtime",
    Description = "Provides distributed workflow runtime capabilities",
    DependsOn = ["WorkflowRuntime", "Resilience"])]
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
    }
}

