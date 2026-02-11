using CShells.Features;
using Elsa.Workflows.Runtime.Handlers;
using Elsa.Workflows.Runtime.Stores;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.ShellFeatures;

/// <summary>
/// Installs and configures workflow runtime caching features.
/// </summary>
[ShellFeature(
    DisplayName = "Caching Workflow Runtime",
    Description = "Provides caching for workflow runtime operations",
    DependsOn = ["MemoryCache", "WorkflowRuntime"])]
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


