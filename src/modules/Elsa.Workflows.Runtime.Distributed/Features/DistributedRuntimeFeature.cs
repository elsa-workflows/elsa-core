using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Resilience.Features;
using Elsa.Workflows.Runtime.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Distributed.Features;

/// <summary>
/// Installs and configures workflow runtime features.
/// </summary>
[DependsOn(typeof(WorkflowRuntimeFeature))]
[DependsOn(typeof(ResilienceFeature))]
public class DistributedRuntimeFeature(IModule module) : FeatureBase(module)
{
    public override void Configure()
    {
        Module.UseWorkflowRuntime(runtime =>
        {
            runtime.WorkflowRuntime = sp => sp.GetRequiredService<DistributedWorkflowRuntime>();
            runtime.BookmarkQueueWorker = sp => sp.GetRequiredService<DistributedBookmarkQueueWorker>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .AddScoped<DistributedWorkflowRuntime>()
            .AddScoped<DistributedBookmarkQueueWorker>()
            
            .Decorate<IWorkflowDefinitionsRefresher, DistributedWorkflowDefinitionsRefresher>()
            .Decorate<IWorkflowDefinitionsReloader, DistributedWorkflowDefinitionsReloader>();
    }
}