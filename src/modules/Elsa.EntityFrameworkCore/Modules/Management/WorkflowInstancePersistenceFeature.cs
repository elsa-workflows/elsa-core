using Elsa.EntityFrameworkCore.Common;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Modules.Management;

[DependsOn(typeof(WorkflowManagementFeature))]
public class EFCoreWorkflowInstancePersistenceFeature : PersistenceFeatureBase<ManagementElsaDbContext>
{
    public EFCoreWorkflowInstancePersistenceFeature(IModule module) : base(module)
    {
    }

    public override void Configure()
    {
        Module.Configure<WorkflowManagementFeature>(feature =>
        {
            feature.WorkflowInstanceStore = sp => sp.GetRequiredService<EFCoreWorkflowInstanceStore>();
        });
    }

    public override void Apply()
    {
        base.Apply();

        AddStore<WorkflowInstance, EFCoreWorkflowInstanceStore>();
    }
}