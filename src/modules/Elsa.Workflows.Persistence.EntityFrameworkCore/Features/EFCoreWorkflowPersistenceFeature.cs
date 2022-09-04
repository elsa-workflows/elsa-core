using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Persistence.EntityFrameworkCore.Common.Abstractions;
using Elsa.Persistence.EntityFrameworkCore.Common.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Features;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Handlers;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Implementations;
using Elsa.Workflows.Runtime.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Features;

[DependsOn(typeof(WorkflowRuntimeFeature))]
public class EFCoreWorkflowPersistenceFeature : EFCorePersistenceFeature<WorkflowsDbContext>
{
    public EFCoreWorkflowPersistenceFeature(IModule module) : base(module)
    {
    }

    public override void Configure()
    {
        Module.Configure<WorkflowManagementFeature>(feature =>
        {
            feature.WorkflowDefinitionStore = sp => sp.GetRequiredService<EFCoreWorkflowDefinitionStore>();
            feature.WorkflowInstanceStore = sp => sp.GetRequiredService<EFCoreWorkflowInstanceStore>();
            feature.WorkflowTriggerStore = sp => sp.GetRequiredService<EFCoreWorkflowTriggerStore>();
            feature.WorkflowExecutionLogStore = sp => sp.GetRequiredService<EFCoreWorkflowExecutionLogStore>();
        });
        
        Module.Configure<WorkflowRuntimeFeature>(feature => feature.WorkflowStateStore = sp => sp.GetRequiredService<EFCoreWorkflowStateStore>());
    }

    public override void Apply()
    {
        base.Apply();

        AddStore<WorkflowDefinition, EFCoreWorkflowDefinitionStore>(Services);
        AddStore<WorkflowInstance, EFCoreWorkflowInstanceStore>(Services);
        AddStore<WorkflowTrigger, EFCoreWorkflowTriggerStore>(Services);
        AddStore<WorkflowExecutionLogRecord, EFCoreWorkflowExecutionLogStore>(Services);
        Services.AddSingleton<EFCoreWorkflowStateStore>();

        Services
            .AddSingleton<IEntitySerializer<WorkflowsDbContext, WorkflowDefinition>, WorkflowDefinitionSerializer>()
            .AddSingleton<IEntitySerializer<WorkflowsDbContext, WorkflowInstance>, WorkflowInstanceSerializer>()
            .AddSingleton<IEntitySerializer<WorkflowsDbContext, WorkflowExecutionLogRecord>, WorkflowExecutionLogRecordSerializer>()
            ;
    }
}