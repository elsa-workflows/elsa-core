using Elsa.Features.Services;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions to the specified <see cref="IModule"/>/
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Adds the workflow management feature to the specified module. 
    /// </summary>
    public static IModule UseWorkflowManagement(this IModule module, Action<WorkflowManagementFeature>? configure = default)
    {
        module.Configure<WorkflowManagementFeature>(management =>
        {
            management.AddActivity<NotFoundActivity>();
            configure?.Invoke(management);
        });
        return module;
    }
    
    /// <summary>
    /// Adds the default workflow management feature to the specified module. 
    /// </summary>
    public static WorkflowManagementFeature UseWorkflowDefinitions(this WorkflowManagementFeature feature, Action<WorkflowDefinitionsFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
    
    /// <summary>
    /// Adds the workflow instance feature to workflow management module. 
    /// </summary>
    public static WorkflowManagementFeature UseWorkflowInstances(this WorkflowManagementFeature feature, Action<WorkflowInstancesFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }

    /// <summary>
    /// Adds all types implementing <see cref="IActivity"/> to the system.
    /// </summary>
    public static IModule AddActivitiesFrom<TMarkerType>(this IModule module) => module.UseWorkflowManagement(management => management.AddActivitiesFrom<TMarkerType>());
    
    /// <summary>
    /// Adds the specified activity type to the system.
    /// </summary>
    public static IModule AddActivity<T>(this IModule module) where T:IActivity => module.UseWorkflowManagement(management => management.AddActivity<T>());
}