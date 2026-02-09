using CShells.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Distributed.ShellFeatures;

/// <summary>
/// Installs and configures distributed workflow runtime features.
/// </summary>
[ShellFeature(
    DisplayName = "Distributed Workflow Runtime",
    Description = "Provides distributed workflow execution and queue processing capabilities",
    DependsOn = ["WorkflowRuntime", "Resilience"])]
public class DistributedRuntimeFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddScoped<DistributedWorkflowRuntime>()
            .AddScoped<DistributedBookmarkQueueWorker>();
    }
}
