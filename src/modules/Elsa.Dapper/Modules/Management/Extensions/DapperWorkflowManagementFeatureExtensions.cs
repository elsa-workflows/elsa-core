using Elsa.Dapper.Modules.Management.Features;
using Elsa.Workflows.Management.Features;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

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

    /// <summary>
    /// Sets up the EF Core persistence provider. 
    /// </summary>
    public static WorkflowInstancesFeature UseDapper(this WorkflowInstancesFeature feature, Action<DapperWorkflowInstancePersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
    
    /// <summary>
    /// Sets up the Dapper persistence provider. 
    /// </summary>
    public static WorkflowManagementFeature UseDapper(this WorkflowManagementFeature feature, Action<DapperWorkflowManagementPersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}