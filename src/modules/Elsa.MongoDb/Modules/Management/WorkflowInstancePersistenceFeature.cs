using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MongoDb.Common;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MongoDb.Modules.Management;

/// <summary>
/// Configures the <see cref="WorkflowInstancesFeature"/> feature with a MongoDb persistence provider.
/// </summary>
[DependsOn(typeof(WorkflowManagementFeature))]
public class MongoWorkflowInstancePersistenceFeature : PersistenceFeatureBase
{
    /// <inheritdoc />
    public MongoWorkflowInstancePersistenceFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowInstancesFeature>(feature => feature.WorkflowInstanceStore = sp => sp.GetRequiredService<MongoWorkflowInstanceStore>());
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        
        AddCollection<WorkflowInstance>("workflow_instances");

        AddStore<WorkflowInstance, MongoWorkflowInstanceStore>();
        
        Services.AddHostedService<CreateIndices>();
    }
}