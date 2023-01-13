using Elsa.Extensions;
using Elsa.Workflows.Management.Features;

namespace Elsa.EntityFrameworkCore.Modules.Management;

/// <summary>
/// Provides extensions to <see cref="WorkflowManagementFeature"/>.
/// </summary>
public static class WorkflowManagementFeatureExtensions
{
    /// <summary>
    /// Sets up the EF Core persistence provider. 
    /// </summary>
    public static WorkflowDefinitionsFeature UseEntityFrameworkCore(this WorkflowDefinitionsFeature feature, Action<EFCoreWorkflowDefinitionPersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }

    /// <summary>
    /// Sets up the EF Core persistence provider. 
    /// </summary>
    public static WorkflowInstancesFeature UseEntityFrameworkCore(this WorkflowInstancesFeature feature, Action<EFCoreWorkflowInstancePersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
    
    /// <summary>
    /// Sets up the EF Core persistence provider. 
    /// </summary>
    public static WorkflowManagementFeature UseEntityFrameworkCore(this WorkflowManagementFeature feature, Action<EFCoreWorkflowManagementPersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}