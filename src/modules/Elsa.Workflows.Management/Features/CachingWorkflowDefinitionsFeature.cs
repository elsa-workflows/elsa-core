using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Handlers;
using Elsa.Workflows.Management.Handlers.Notification;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Management.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Features;

/// <summary>
/// Configures workflow definition caching.
/// </summary>
public class CachingWorkflowDefinitionsFeature : FeatureBase
{
    /// <inheritdoc />
    public CachingWorkflowDefinitionsFeature(IModule module) : base(module)
    {
    }
    
    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSingleton<IWorkflowDefinitionCacheManager, WorkflowDefinitionCacheManager>();
        Services.Decorate<IWorkflowDefinitionStore, CachingWorkflowDefinitionStore>();
        Services.Decorate<IWorkflowDefinitionService, CachingWorkflowDefinitionService>();
        Services.AddNotificationHandler<EvictWorkflowDefinitionServiceCache>();
    }
}