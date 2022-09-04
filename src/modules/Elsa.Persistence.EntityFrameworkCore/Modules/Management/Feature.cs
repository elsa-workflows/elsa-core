using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Persistence.EntityFrameworkCore.Common;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.Management;

[DependsOn(typeof(WorkflowManagementFeature))]
public class EFCoreManagementPersistenceFeature : PersistenceFeatureBase<ManagementDbContext>
{
    public EFCoreManagementPersistenceFeature(IModule module) : base(module)
    {
    }

    public override void Configure()
    {
        Module.Configure<WorkflowManagementFeature>(feature =>
        {
            feature.WorkflowDefinitionStore = sp => sp.GetRequiredService<EFCoreWorkflowDefinitionStore>();
            feature.WorkflowInstanceStore = sp => sp.GetRequiredService<EFCoreWorkflowInstanceStore>();
        });
    }

    public override void Apply()
    {
        base.Apply();

        AddStore<WorkflowDefinition, EFCoreWorkflowDefinitionStore>();
        AddStore<WorkflowInstance, EFCoreWorkflowInstanceStore>();
    }
}