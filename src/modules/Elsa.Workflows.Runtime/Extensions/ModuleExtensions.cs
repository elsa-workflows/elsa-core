using Elsa.Features.Services;
using Elsa.Workflows.Core.Contracts;
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
    /// Enables the <see cref="WorkflowRuntimeFeature"/> and configures it to use the default workflow runtime.
    /// </summary>
    public static IModule UseDefaultWorkflowRuntime(this IModule module, Action<WorkflowRuntimeFeature>? configureRuntime = default, Action<DefaultWorkflowRuntimeFeature>? configureDefaultRuntime = default)
    {
        module.Configure(configureRuntime);
        module.Configure(configureDefaultRuntime);
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
    /// Configures the execution log records feature.
    /// </summary>
    /// <param name="feature">The workflow runtime feature.</param>
    /// <param name="configure">A callback that configures the execution log records feature.</param>
    /// <returns>The workflow runtime feature.</returns>
    public static WorkflowRuntimeFeature UseExecutionLogRecords(this WorkflowRuntimeFeature feature, Action<ExecutionLogRecordFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
    
    /// <summary>
    /// Configures the workflow state exporter feature.
    /// </summary>
    /// <param name="feature">The workflow runtime feature.</param>
    /// <param name="configure">A callback that configures the workflow state exporter feature.</param>
    /// <returns>The workflow runtime feature.</returns>
    public static WorkflowRuntimeFeature UseAsyncWorkflowStateExporter(this WorkflowRuntimeFeature feature, Action<AsyncWorkflowStateExporterFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}