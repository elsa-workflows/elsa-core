using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Handlers;
using Elsa.Persistence.EntityFrameworkCore.Implementations;
using Elsa.Persistence.EntityFrameworkCore.Services;
using Elsa.Persistence.Options;
using Elsa.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore.Options;

public class EFCorePersistenceOptions : IConfigurator
{
    public EFCorePersistenceOptions(PersistenceOptions persistenceOptions)
    {
        PersistenceOptions = persistenceOptions;

        persistenceOptions
            .WithWorkflowDefinitionStore(sp => sp.GetRequiredService<EFCoreWorkflowDefinitionStore>())
            .WithWorkflowInstanceStore(sp => sp.GetRequiredService<EFCoreWorkflowInstanceStore>())
            .WithWorkflowBookmarkStore(sp => sp.GetRequiredService<EFCoreWorkflowBookmarkStore>())
            .WithWorkflowTriggerStore(sp => sp.GetRequiredService<EFCoreWorkflowTriggerStore>())
            .WithWorkflowExecutionLogStore(sp => sp.GetRequiredService<EFCoreWorkflowExecutionLogStore>())
            ;
    }

    public PersistenceOptions PersistenceOptions { get; }
    public bool ContextPoolingIsEnabled { get; set; }
    public ServiceLifetime DbContextFactoryLifetime { get; set; } = ServiceLifetime.Singleton;
    public Action<IServiceProvider, DbContextOptionsBuilder> DbContextOptionsBuilderAction = (_, _) => { };

    public EFCorePersistenceOptions WithContextPooling(bool enabled = true)
    {
        ContextPoolingIsEnabled = enabled;
        return this;
    }

    public EFCorePersistenceOptions ConfigureDbContextOptions(Action<IServiceProvider, DbContextOptionsBuilder> configure)
    {
        DbContextOptionsBuilderAction = configure;
        return this;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        if (ContextPoolingIsEnabled)
            services.AddPooledDbContextFactory<ElsaDbContext>(DbContextOptionsBuilderAction);
        else
            services.AddDbContextFactory<ElsaDbContext>(DbContextOptionsBuilderAction, DbContextFactoryLifetime);

        services
            .AddSingleton<IStore<WorkflowDefinition>, EFCoreStore<WorkflowDefinition>>()
            .AddSingleton<IStore<WorkflowInstance>, EFCoreStore<WorkflowInstance>>()
            .AddSingleton<IStore<WorkflowBookmark>, EFCoreStore<WorkflowBookmark>>()
            .AddSingleton<IStore<WorkflowTrigger>, EFCoreStore<WorkflowTrigger>>()
            .AddSingleton<IStore<WorkflowExecutionLogRecord>, EFCoreStore<WorkflowExecutionLogRecord>>()
            .AddSingleton<EFCoreWorkflowInstanceStore>()
            .AddSingleton<EFCoreWorkflowDefinitionStore>()
            .AddSingleton<EFCoreWorkflowTriggerStore>()
            .AddSingleton<EFCoreWorkflowBookmarkStore>()
            .AddSingleton<EFCoreWorkflowExecutionLogStore>()
            .AddSingleton<IEntitySerializer<WorkflowDefinition>, WorkflowDefinitionSerializer>()
            .AddSingleton<IEntitySerializer<WorkflowInstance>, WorkflowInstanceSerializer>()
            .AddSingleton<IEntitySerializer<WorkflowExecutionLogRecord>, WorkflowExecutionLogRecordSerializer>()
            ;
    }
}