using Elsa.Workflows.Management.Features;

namespace Elsa.Persistence.EFCore.Modules.Management;

/// <summary>
/// Provides extensions to various management related features.
/// </summary>
public static class WorkflowManagementFeatureExtensions
{
    /// <summary>
    /// Sets up the EF Core persistence provider. 
    /// </summary>
    public static WorkflowDefinitionsFeature UseEntityFrameworkCore(this WorkflowDefinitionsFeature feature, Action<EFCoreWorkflowDefinitionPersistenceFeature>? configure = null)
    {
        feature.Module.Configure(configure);
        return feature;
    }

    /// <summary>
    /// Sets up the EF Core persistence provider. 
    /// </summary>
    public static WorkflowInstancesFeature UseEntityFrameworkCore(this WorkflowInstancesFeature feature, Action<EFCoreWorkflowInstancePersistenceFeature>? configure = null)
    {
        feature.Module.Configure(configure);
        return feature;
    }
    
    /// <summary>
    /// Sets up the EF Core persistence provider. 
    /// </summary>
    public static WorkflowManagementFeature UseEntityFrameworkCore(this WorkflowManagementFeature feature, Action<WorkflowManagementPersistenceFeature>? configure = null)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}