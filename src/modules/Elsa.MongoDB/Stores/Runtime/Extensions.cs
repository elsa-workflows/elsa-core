using Elsa.Workflows.Runtime.Features;

namespace Elsa.MongoDB.Stores.Runtime;

/// <summary>
/// Provides extensions to the <see cref="WorkflowRuntimeFeature"/> feature.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Configures the <see cref="WorkflowRuntimeFeature"/> to use the <see cref="MongoWorkflowRuntimePersistenceFeature"/>.
    /// </summary>
    public static WorkflowRuntimeFeature UseMongoDb(this WorkflowRuntimeFeature feature, Action<MongoWorkflowRuntimePersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="DefaultWorkflowRuntimeFeature"/> to use the <see cref="MongoDefaultWorkflowRuntimePersistenceFeature"/>.
    /// </summary>
    public static DefaultWorkflowRuntimeFeature UseMongoDb(this DefaultWorkflowRuntimeFeature feature, Action<MongoDefaultWorkflowRuntimePersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
        
    /// <summary>
    /// Configures the <see cref="ExecutionLogRecordFeature"/> to use the <see cref="MongoWorkflowExecutionLogStore"/>.
    /// </summary>
    public static ExecutionLogRecordFeature UseMongoDb(this ExecutionLogRecordFeature feature, Action<MongoExecutionLogRecordPersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}