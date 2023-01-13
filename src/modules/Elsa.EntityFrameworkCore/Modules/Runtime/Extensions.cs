using Elsa.Workflows.Runtime.Features;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <summary>
/// Provides extensions to the <see cref="WorkflowRuntimeFeature"/> feature.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Configures the <see cref="DefaultRuntimeFeature"/> to use the <see cref="EFCoreDefaultRuntimePersistenceFeature"/>.
    /// </summary>
    public static DefaultRuntimeFeature UseEntityFrameworkCore(this DefaultRuntimeFeature feature, Action<EFCoreDefaultRuntimePersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
        
    /// <summary>
    /// Configures the <see cref="ExecutionLogRecordFeature"/> to use the <see cref="EFCoreWorkflowExecutionLogStore"/>.
    /// </summary>
    public static ExecutionLogRecordFeature UseEntityFrameworkCore(this ExecutionLogRecordFeature feature, Action<EFCoreExecutionLogRecordPersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}