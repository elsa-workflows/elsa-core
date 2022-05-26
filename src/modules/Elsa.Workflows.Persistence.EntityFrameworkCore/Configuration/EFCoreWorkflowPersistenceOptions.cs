using Elsa.Persistence.Common.Entities;
using Elsa.Persistence.EntityFrameworkCore.Common.HostedServices;
using Elsa.Persistence.EntityFrameworkCore.Common.Implementations;
using Elsa.Persistence.EntityFrameworkCore.Common.Services;
using Elsa.ServiceConfiguration.Abstractions;
using Elsa.ServiceConfiguration.Services;
using Elsa.Workflows.Persistence.Configuration;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Handlers;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Configuration;

public class EFCoreWorkflowPersistenceOptions : ConfiguratorBase
{
    public EFCoreWorkflowPersistenceOptions(WorkflowPersistenceConfigurator workflowPersistenceConfigurator)
    {
        WorkflowPersistenceConfigurator = workflowPersistenceConfigurator;

        workflowPersistenceConfigurator
            .WithWorkflowDefinitionStore(sp => sp.GetRequiredService<EFCoreWorkflowDefinitionStore>())
            .WithWorkflowInstanceStore(sp => sp.GetRequiredService<EFCoreWorkflowInstanceStore>())
            .WithWorkflowBookmarkStore(sp => sp.GetRequiredService<EFCoreWorkflowBookmarkStore>())
            .WithWorkflowTriggerStore(sp => sp.GetRequiredService<EFCoreWorkflowTriggerStore>())
            .WithWorkflowExecutionLogStore(sp => sp.GetRequiredService<EFCoreWorkflowExecutionLogStore>())
            ;
    }

    public WorkflowPersistenceConfigurator WorkflowPersistenceConfigurator { get; }
    public bool ContextPoolingIsEnabled { get; set; }
    public bool AutoRunMigrationsIsEnabled { get; set; } = true;
    public ServiceLifetime DbContextFactoryLifetime { get; set; } = ServiceLifetime.Singleton;
    public Action<IServiceProvider, DbContextOptionsBuilder> DbContextOptionsBuilderAction = (_, _) => { };

    public EFCoreWorkflowPersistenceOptions WithContextPooling(bool enabled = true)
    {
        ContextPoolingIsEnabled = enabled;
        return this;
    }

    public EFCoreWorkflowPersistenceOptions AutoRunMigrations(bool enabled = true)
    {
        AutoRunMigrationsIsEnabled = enabled;
        return this;
    }

    public EFCoreWorkflowPersistenceOptions ConfigureDbContextOptions(Action<IServiceProvider, DbContextOptionsBuilder> configure)
    {
        DbContextOptionsBuilderAction = configure;
        return this;
    }

    public override void ConfigureServices(IServiceConfiguration serviceConfiguration)
    {
        var services = serviceConfiguration.Services;

        if (ContextPoolingIsEnabled)
            services.AddPooledDbContextFactory<WorkflowsDbContext>(DbContextOptionsBuilderAction);
        else
            services.AddDbContextFactory<WorkflowsDbContext>(DbContextOptionsBuilderAction, DbContextFactoryLifetime);

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
    }

    public override void ConfigureHostedServices(IServiceConfiguration serviceConfiguration)
    {
        if (AutoRunMigrationsIsEnabled)
            serviceConfiguration.ConfigureHostedService<RunMigrations<WorkflowsDbContext>>(-1); // Migrations need to run before other hosted services that depend on DB access.
    }

    private void AddStore<TEntity, TStore>(IServiceCollection services) where TEntity : Entity where TStore : class
    {
        services
            .AddSingleton<IStore<WorkflowsDbContext, TEntity>, EFCoreStore<WorkflowsDbContext, TEntity>>()
            .AddSingleton<TStore>();
    }
}