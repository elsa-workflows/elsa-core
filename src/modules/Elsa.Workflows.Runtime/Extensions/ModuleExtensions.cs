using Elsa.Features.Services;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Services;
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
    
    public static WorkflowRuntimeFeature UseDefaultRuntime(this WorkflowRuntimeFeature feature, Action<DefaultRuntimeFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
    
    public static WorkflowRuntimeFeature UseExecutionLogRecords(this WorkflowRuntimeFeature feature, Action<ExecutionLogRecordFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
    
    public static WorkflowRuntimeFeature UseAsyncWorkflowStateExporter(this WorkflowRuntimeFeature feature, Action<AsyncWorkflowStateExporterFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}