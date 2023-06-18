using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MongoDb.Common;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MongoDb.Modules.Management;

/// <summary>
/// Configures the <see cref="WorkflowDefinitionsFeature"/> feature with a MongoDb persistence provider.
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
        
        AddCollection<WorkflowDefinition>("workflow_definitions");

        AddStore<WorkflowDefinition, MongoWorkflowDefinitionStore>();
        
        Services.AddHostedService<CreateIndices>();
    }
}