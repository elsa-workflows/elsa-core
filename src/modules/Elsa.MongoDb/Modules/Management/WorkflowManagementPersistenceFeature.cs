using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MongoDb.Common;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MongoDb.Modules.Management;

/// <summary>
/// Configures the <see cref="WorkflowInstancesFeature"/> and <see cref="WorkflowDefinitionsFeature"/> features with a MongoDb persistence provider.
/// </summary>
[DependsOn(typeof(WorkflowManagementFeature))]
[DependsOn(typeof(WorkflowInstancesFeature))]
[DependsOn(typeof(WorkflowDefinitionsFeature))]
[PublicAPI]
public class MongoWorkflowManagementPersistenceFeature : PersistenceFeatureBase
{
    /// <inheritdoc />
    public MongoWorkflowManagementPersistenceFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowInstancesFeature>(feature => feature.WorkflowInstanceStore = sp => sp.GetRequiredService<MongoWorkflowInstanceStore>());
        Module.Configure<WorkflowDefinitionsFeature>(feature => feature.WorkflowDefinitionStore = sp => sp.GetRequiredService<MongoWorkflowDefinitionStore>());
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        
        AddCollection<WorkflowInstance>("workflow_instances");
        AddCollection<WorkflowDefinition>("workflow_definitions");

        AddStore<WorkflowInstance, MongoWorkflowInstanceStore>();
        AddStore<WorkflowDefinition, MongoWorkflowDefinitionStore>();
        
        Services.AddHostedService<CreateIndices>();
    }
}