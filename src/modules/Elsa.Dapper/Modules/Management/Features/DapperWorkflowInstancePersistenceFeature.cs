using Elsa.Dapper.Features;
using Elsa.Dapper.Modules.Management.Stores;
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
[DependsOn(typeof(DapperFeature))]
public class DapperWorkflowInstancePersistenceFeature : FeatureBase
{
    /// <inheritdoc />
    public DapperWorkflowInstancePersistenceFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowInstancesFeature>(feature => { feature.WorkflowInstanceStore = sp => sp.GetRequiredService<DapperWorkflowInstanceStore>(); });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        
        Services.AddSingleton<DapperWorkflowInstanceStore>();
    }
}