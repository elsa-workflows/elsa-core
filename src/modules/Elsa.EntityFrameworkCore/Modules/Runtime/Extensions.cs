using Elsa.Workflows.Runtime.Features;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <summary>
/// Provides extensions to the <see cref="WorkflowRuntimeFeature"/> feature.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Configures the <see cref="WorkflowRuntimeFeature"/> to use the <see cref="EFCoreWorkflowRuntimePersistenceFeature"/>.
    /// </summary>
    public static WorkflowRuntimeFeature UseEntityFrameworkCore(this WorkflowRuntimeFeature feature, Action<EFCoreWorkflowRuntimePersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="DefaultWorkflowRuntimeFeature"/> to use the <see cref="EFCoreDefaultWorkflowRuntimePersistenceFeature"/>.
    /// </summary>
    public static DefaultWorkflowRuntimeFeature UseEntityFrameworkCore(this DefaultWorkflowRuntimeFeature feature, Action<EFCoreDefaultWorkflowRuntimePersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
        
    /// <summary>
    /// Configures the <see cref="ExecutionLogRecordFeature"/> to use the <see cref="EFCoreWorkflowExecutionLogStore"/>.
    /// </summary>
    public static ExecutionLogRecordFeature UseEntityFrameworkCore(this ExecutionLogRecordFeature feature, Action<EFCoreExecutionLogPersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}