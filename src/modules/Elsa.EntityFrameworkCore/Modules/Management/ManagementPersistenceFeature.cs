using Elsa.EntityFrameworkCore.Common;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Modules.Management;

[DependsOn(typeof(WorkflowManagementFeature))]
public class EFCoreManagementPersistenceFeature : PersistenceFeatureBase<ManagementElsaDbContext>
{
    public EFCoreManagementPersistenceFeature(IModule module) : base(module)
    {
    }

    public override void Configure()
    {
        Module.Configure<WorkflowManagementFeature>(feature =>
        {
            feature.WorkflowDefinitionStore = sp => sp.GetRequiredService<EFCoreWorkflowDefinitionStore>();
        });
    }

    public override void Apply()
    {
        base.Apply();

        AddStore<WorkflowDefinition, EFCoreWorkflowDefinitionStore>();
    }
}