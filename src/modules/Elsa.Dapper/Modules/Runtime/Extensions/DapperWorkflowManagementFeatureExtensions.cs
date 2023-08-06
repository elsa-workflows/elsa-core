using Elsa.Dapper.Modules.Runtime.Features;
using Elsa.Dapper.Modules.Runtime.Stores;
using Elsa.Workflows.Runtime.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions to the <see cref="WorkflowRuntimeFeature"/> feature.
/// </summary>
public static class DapperWorkflowRuntimeExtensions
{
    /// <summary>
    /// Configures the <see cref="WorkflowRuntimeFeature"/> to use the <see cref="DapperWorkflowRuntimePersistenceFeature"/>.
    /// </summary>
    public static WorkflowRuntimeFeature UseDapper(this WorkflowRuntimeFeature feature, Action<DapperWorkflowRuntimePersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="DefaultWorkflowRuntimeFeature"/> to use the <see cref="DapperDefaultWorkflowRuntimePersistenceFeature"/>.
    /// </summary>
    public static DefaultWorkflowRuntimeFeature UseDapper(this DefaultWorkflowRuntimeFeature feature, Action<DapperDefaultWorkflowRuntimePersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
        
    /// <summary>
    /// Configures the <see cref="ExecutionLogRecordFeature"/> to use the <see cref="DapperWorkflowExecutionLogStore"/>.
    /// </summary>
    public static ExecutionLogRecordFeature UseDapper(this ExecutionLogRecordFeature feature, Action<EFCoreExecutionLogPersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}