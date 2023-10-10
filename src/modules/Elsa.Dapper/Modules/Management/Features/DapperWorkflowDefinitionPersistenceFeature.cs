using Elsa.Dapper.Features;
using Elsa.Dapper.Modules.Management.Stores;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dapper.Modules.Management.Features;

/// <summary>
/// Configures the <see cref="WorkflowDefinitionsFeature"/> feature with an Entity Framework Core persistence provider.
/// </summary>
[DependsOn(typeof(WorkflowManagementFeature))]
[DependsOn(typeof(DapperFeature))]
public class DapperWorkflowDefinitionPersistenceFeature : FeatureBase
{
    /// <inheritdoc />
    public DapperWorkflowDefinitionPersistenceFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowDefinitionsFeature>(feature =>
        {
            feature.WorkflowDefinitionStore = sp => sp.GetRequiredService<DapperWorkflowDefinitionStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        
        Services.AddSingleton<DapperWorkflowDefinitionStore>();
    }
}