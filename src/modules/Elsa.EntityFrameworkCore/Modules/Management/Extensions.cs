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
    public static WorkflowManagementFeature UseEntityFrameworkCore(this WorkflowManagementFeature feature, Action<EFCoreManagementPersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}