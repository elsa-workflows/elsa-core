using Elsa.Dapper.Modules.Management.Features;
using Elsa.Workflows.Management.Features;
using JetBrains.Annotations;

namespace Elsa.Dapper.Modules.Management.Extensions;

/// <summary>
/// Provides extensions to various management related features.
/// </summary>
[PublicAPI]
public static class DapperWorkflowManagementFeatureExtensions
{
    /// <summary>
    /// Sets up the EF Core persistence provider. 
    /// </summary>
    public static WorkflowDefinitionsFeature UseDapper(this WorkflowDefinitionsFeature feature, Action<DapperWorkflowDefinitionPersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }

    // /// <summary>
    // /// Sets up the EF Core persistence provider. 
    // /// </summary>
    // public static WorkflowInstancesFeature UseEntityFrameworkCore(this WorkflowInstancesFeature feature, Action<EFCoreWorkflowInstancePersistenceFeature>? configure = default)
    // {
    //     feature.Module.Configure(configure);
    //     return feature;
    // }
    //
    // /// <summary>
    // /// Sets up the EF Core persistence provider. 
    // /// </summary>
    // public static WorkflowManagementFeature UseEntityFrameworkCore(this WorkflowManagementFeature feature, Action<EFCoreWorkflowManagementPersistenceFeature>? configure = default)
    // {
    //     feature.Module.Configure(configure);
    //     return feature;
    // }
}