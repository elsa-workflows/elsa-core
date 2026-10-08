using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Features;
using Elsa.Persistence.Dapper.Modules.Management.Records;
using Elsa.Persistence.Dapper.Modules.Management.Stores;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Dapper.Modules.Management.Features;

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
        
        Services.AddDapperStore<DapperWorkflowDefinitionStore, WorkflowDefinitionRecord>("WorkflowDefinitions");
    }
}