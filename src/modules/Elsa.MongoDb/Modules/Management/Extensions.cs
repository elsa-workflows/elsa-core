using Elsa.Workflows.Management.Features;

namespace Elsa.MongoDb.Modules.Management;

/// <summary>
/// Provides extensions to <see cref="WorkflowManagementFeature"/>.
/// </summary>
public static class WorkflowManagementFeatureExtensions
{
    /// <summary>
    /// Sets up the MongoDb persistence provider. 
    /// </summary>
    public static WorkflowDefinitionsFeature UseMongoDb(this WorkflowDefinitionsFeature feature, Action<MongoWorkflowDefinitionPersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }

    /// <summary>
    /// Sets up the MongoDb persistence provider. 
    /// </summary>
    public static WorkflowInstancesFeature UseMongoDb(this WorkflowInstancesFeature feature, Action<MongoWorkflowInstancePersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
    
    /// <summary>
    /// Sets up the MongoDb persistence provider. 
    /// </summary>
    public static WorkflowManagementFeature UseMongoDb(this WorkflowManagementFeature feature, Action<MongoWorkflowManagementPersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}