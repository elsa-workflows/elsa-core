using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Modules.Management;

/// <summary>
/// Configures the <see cref="WorkflowInstancesFeature"/> feature with an Entity Framework Core persistence provider.
/// </summary>
[DependsOn(typeof(WorkflowManagementFeature))]
[DependsOn(typeof(WorkflowInstancesFeature))]
public class EFCoreWorkflowInstancePersistenceFeature(IModule module) : PersistenceFeatureBase<EFCoreWorkflowInstancePersistenceFeature, ManagementElsaDbContext>(module)
{
    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowInstancesFeature>(feature => feature.WorkflowInstanceStore = sp => sp.GetRequiredService<EFCoreWorkflowInstanceStore>());
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        AddEntityStore<WorkflowInstance, EFCoreWorkflowInstanceStore>();
    }
}