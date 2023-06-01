using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MongoDB.Common;
using Elsa.MongoDB.Stores.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MongoDB.Modules.Management;

/// <summary>
/// Configures the <see cref="WorkflowDefinitionsFeature"/> feature with an MongoDb persistence provider.
/// </summary>
[DependsOn(typeof(WorkflowManagementFeature))]
public class MongoWorkflowDefinitionPersistenceFeature : PersistenceFeatureBase
{
    /// <inheritdoc />
    public MongoWorkflowDefinitionPersistenceFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowDefinitionsFeature>(feature =>
        {
            feature.WorkflowDefinitionStore = sp => sp.GetRequiredService<MongoWorkflowDefinitionStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();

        AddStore<WorkflowDefinition, MongoWorkflowDefinitionStore>();
    }
}