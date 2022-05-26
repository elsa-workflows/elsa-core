using Elsa.Persistence.EntityFrameworkCore.Common.Abstractions;
using Elsa.Persistence.EntityFrameworkCore.Common.Services;
using Elsa.ServiceConfiguration.Services;
using Elsa.Workflows.Persistence.Configuration;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Handlers;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Configuration;

public class EFCoreWorkflowPersistenceConfigurator : EFCorePersistenceConfigurator<WorkflowsDbContext>
{
    public EFCoreWorkflowPersistenceConfigurator(IServiceConfiguration serviceConfiguration) : base(serviceConfiguration)
    {
    }

    public override void ConfigureServices(IServiceConfiguration serviceConfiguration)
    {
        base.ConfigureServices(serviceConfiguration);

        var services = serviceConfiguration.Services;
        AddStore<WorkflowDefinition, EFCoreWorkflowDefinitionStore>(services);
        AddStore<WorkflowInstance, EFCoreWorkflowInstanceStore>(services);
        AddStore<WorkflowBookmark, EFCoreWorkflowBookmarkStore>(services);
        AddStore<WorkflowTrigger, EFCoreWorkflowTriggerStore>(services);
        AddStore<WorkflowExecutionLogRecord, EFCoreWorkflowExecutionLogStore>(services);

        services
            .AddSingleton<IEntitySerializer<WorkflowsDbContext, WorkflowDefinition>, WorkflowDefinitionSerializer>()
            .AddSingleton<IEntitySerializer<WorkflowsDbContext, WorkflowInstance>, WorkflowInstanceSerializer>()
            .AddSingleton<IEntitySerializer<WorkflowsDbContext, WorkflowExecutionLogRecord>, WorkflowExecutionLogRecordSerializer>()
            ;

        ServiceConfiguration.Configure<WorkflowPersistenceConfigurator>().WithWorkflowDefinitionStore(sp => sp.GetRequiredService<EFCoreWorkflowDefinitionStore>())
            .WithWorkflowInstanceStore(sp => sp.GetRequiredService<EFCoreWorkflowInstanceStore>())
            .WithWorkflowBookmarkStore(sp => sp.GetRequiredService<EFCoreWorkflowBookmarkStore>())
            .WithWorkflowTriggerStore(sp => sp.GetRequiredService<EFCoreWorkflowTriggerStore>())
            .WithWorkflowExecutionLogStore(sp => sp.GetRequiredService<EFCoreWorkflowExecutionLogStore>())
            ;
    }
}