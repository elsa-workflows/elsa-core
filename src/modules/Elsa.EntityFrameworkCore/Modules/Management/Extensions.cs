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
    public static DefaultManagementFeature UseEntityFrameworkCore(this DefaultManagementFeature feature,
        Action<EFCoreDefaultManagementPersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }

    /// <summary>
    /// Sets up the EF Core persistence provider. 
    /// </summary>
    public static WorkflowInstanceFeature UseEntityFrameworkCore(this WorkflowInstanceFeature feature, Action<EFCoreWorkflowInstancePersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}