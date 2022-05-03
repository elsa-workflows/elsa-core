using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Handlers;
using Elsa.Persistence.EntityFrameworkCore.HostedServices;
using Elsa.Persistence.EntityFrameworkCore.Implementations;
using Elsa.Persistence.EntityFrameworkCore.Services;
using Elsa.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEntityFrameworkCorePersistence(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> configure,
        bool useContextPooling = true)
    {
        services.AddElsaDbContextFactory(configure, useContextPooling);

        services
            .AddSingleton<IStore<WorkflowDefinition>, EFCoreStore<WorkflowDefinition>>()
            .AddSingleton<IStore<WorkflowInstance>, EFCoreStore<WorkflowInstance>>()
            .AddSingleton<IStore<WorkflowBookmark>, EFCoreStore<WorkflowBookmark>>()
            .AddSingleton<IStore<WorkflowTrigger>, EFCoreStore<WorkflowTrigger>>()
            .AddSingleton<IStore<WorkflowExecutionLogRecord>, EFCoreStore<WorkflowExecutionLogRecord>>()
            .AddSingleton<IWorkflowInstanceStore, EFCoreWorkflowInstanceStore>()
            .AddSingleton<IWorkflowDefinitionStore, EFCoreWorkflowDefinitionStore>()
            .AddSingleton<IWorkflowTriggerStore, EFCoreWorkflowTriggerStore>()
            .AddSingleton<IWorkflowBookmarkStore, EFCoreWorkflowBookmarkStore>()
            .AddSingleton<IWorkflowExecutionLogStore, EFCoreWorkflowExecutionLogStore>()
            .AddSingleton<IEntitySerializer<WorkflowDefinition>, WorkflowDefinitionSerializer>()
            .AddSingleton<IEntitySerializer<WorkflowInstance>, WorkflowInstanceSerializer>()
            .AddSingleton<IEntitySerializer<WorkflowExecutionLogRecord>, WorkflowExecutionLogRecordSerializer>()
            ;

        return services;
    }

    public static IServiceCollection AutoRunMigrations(this IServiceCollection services)
    {
        return services.AddHostedService<RunMigrations>();
    }

    private static IServiceCollection AddElsaDbContextFactory(this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> configure,
        bool useContextPooling,
        ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
    {
        if (useContextPooling)
            services.AddPooledDbContextFactory<ElsaDbContext>(configure);
        else
            services.AddDbContextFactory<ElsaDbContext>(configure, serviceLifetime);

        return services;
    }
}