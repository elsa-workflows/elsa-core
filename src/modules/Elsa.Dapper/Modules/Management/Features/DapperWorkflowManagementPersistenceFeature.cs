using Elsa.Dapper.Contracts;
using Elsa.Dapper.Features;
using Elsa.Dapper.Modules.Management.Services;
using Elsa.Dapper.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dapper.Modules.Management.Features;

/// <summary>
/// Configures the <see cref="WorkflowInstancesFeature"/> and <see cref="WorkflowDefinitionsFeature"/> features with a Dapper persistence provider.
/// </summary>
[DependsOn(typeof(WorkflowManagementFeature))]
[DependsOn(typeof(WorkflowInstancesFeature))]
[DependsOn(typeof(WorkflowDefinitionsFeature))]
[DependsOn(typeof(CommonFeature))]
[PublicAPI]
public class DapperWorkflowManagementPersistenceFeature : FeatureBase
{
    /// <inheritdoc />
    public DapperWorkflowManagementPersistenceFeature(IModule module) : base(module)
    {
    }
    
    /// <summary>
    /// Gets or sets a factory that provides an <see cref="IDbConnectionProvider"/> instance.
    /// </summary>
    public Func<IServiceProvider, IDbConnectionProvider> DbConnectionProvider { get; set; } = _ => new SqliteDbConnectionProvider();

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowInstancesFeature>(feature => feature.WorkflowInstanceStore = sp => sp.GetRequiredService<DapperWorkflowInstanceStore>());
        Module.Configure<WorkflowDefinitionsFeature>(feature => feature.WorkflowDefinitionStore = sp => sp.GetRequiredService<DapperWorkflowDefinitionStore>());
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();

        Services.AddSingleton(DbConnectionProvider);
        Services.AddSingleton<DapperWorkflowInstanceStore>();
        Services.AddSingleton<DapperWorkflowDefinitionStore>();
    }
}