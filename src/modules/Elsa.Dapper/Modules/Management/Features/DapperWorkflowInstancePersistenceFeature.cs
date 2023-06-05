using Elsa.Dapper.Contracts;
using Elsa.Dapper.Features;
using Elsa.Dapper.Modules.Management.Services;
using Elsa.Dapper.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dapper.Modules.Management.Features;

/// <summary>
/// Configures the <see cref="WorkflowInstancesFeature"/> feature with an Entity Framework Core persistence provider.
/// </summary>
[DependsOn(typeof(WorkflowManagementFeature))]
[DependsOn(typeof(CommonFeature))]
public class DapperWorkflowInstancePersistenceFeature : FeatureBase
{
    /// <inheritdoc />
    public DapperWorkflowInstancePersistenceFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// Gets or sets a factory that provides an <see cref="IDbConnectionProvider"/> instance.
    /// </summary>
    public Func<IServiceProvider, IDbConnectionProvider> DbConnectionProvider { get; set; } = _ => new SqliteDbConnectionProvider();

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowInstancesFeature>(feature => { feature.WorkflowInstanceStore = sp => sp.GetRequiredService<DapperWorkflowInstanceStore>(); });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();

        Services.AddSingleton(DbConnectionProvider);
        Services.AddSingleton<DapperWorkflowInstanceStore>();
    }
}