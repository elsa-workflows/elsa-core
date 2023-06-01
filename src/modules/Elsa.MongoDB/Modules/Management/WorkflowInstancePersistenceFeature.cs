using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MongoDB.Common;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MongoDB.Modules.Management;

/// <summary>
/// Configures the <see cref="WorkflowInstancesFeature"/> feature with an MongoDb persistence provider.
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

        AddStore<WorkflowInstance, MongoWorkflowInstanceStore>();
    }
}