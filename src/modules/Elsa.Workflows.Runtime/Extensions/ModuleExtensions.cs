using System.Reflection;
using Elsa.Features.Services;
using Elsa.Workflows;
using Elsa.Workflows.Runtime.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extensions to setup the <see cref="WorkflowRuntimeFeature"/> feature.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Enables the <see cref="WorkflowRuntimeFeature"/> feature.
    /// </summary>
    public static IModule UseWorkflowRuntime(this IModule module, Action<WorkflowRuntimeFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }

    /// <summary>
    /// Register the specified workflow type.
    /// </summary>
    public static IModule AddWorkflow<T>(this IModule module) where T : IWorkflow
    {
        module.Configure<WorkflowRuntimeFeature>().AddWorkflow<T>();
        return module;
    }

    /// <summary>
    /// Register all workflows contained in the assembly containing the specified marker type.
    /// </summary>
    public static IModule AddWorkflowsFrom<TMarker>(this IModule module) => module.AddWorkflowsFrom(typeof(TMarker).Assembly);
    
    /// <summary>
    /// Register all workflows in the specified assembly.
    /// </summary>
    public static IModule AddWorkflowsFrom(this IModule module, Assembly assembly)
    {
        module.Configure<WorkflowRuntimeFeature>().AddWorkflowsFrom(assembly);
        return module;
    }
    
    /// <summary>
    /// Configures the default workflow runtime.
    /// </summary>
    /// <param name="feature">The workflow runtime feature.</param>
    /// <param name="configure">A callback that configures the default workflow runtime.</param>
    /// <returns>The workflow runtime feature.</returns>
    public static WorkflowRuntimeFeature UseDefaultRuntime(this WorkflowRuntimeFeature feature, Action<DefaultWorkflowRuntimeFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
    
    /// <summary>
    /// Adds caching stores feature to the workflow runtime feature.
    /// </summary>
    public static WorkflowRuntimeFeature UseCache(this WorkflowRuntimeFeature feature, Action<CachingWorkflowRuntimeFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}