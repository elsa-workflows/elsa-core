using Elsa.Mediator.Extensions;
using Elsa.Persistence.Entities;
using Elsa.Persistence.EntityFrameworkCore.Contracts;
using Elsa.Persistence.EntityFrameworkCore.Handlers.Commands;
using Elsa.Persistence.EntityFrameworkCore.Handlers.Serialization;
using Elsa.Persistence.EntityFrameworkCore.HostedServices;
using Elsa.Persistence.EntityFrameworkCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEntityFrameworkCorePersistence(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> configure,
        bool useContextPooling = true,
        bool autoRunMigrations = true)
    {
        services.AddElsaDbContextFactory(configure, useContextPooling);

        services
            .AddEFCoreHandlers()
            .AddSingleton<IStore<WorkflowDefinition>, EFCoreStore<WorkflowDefinition>>()
            .AddSingleton<IStore<WorkflowInstance>, EFCoreStore<WorkflowInstance>>()
            .AddSingleton<IStore<WorkflowBookmark>, EFCoreStore<WorkflowBookmark>>()
            .AddSingleton<IStore<WorkflowTrigger>, EFCoreStore<WorkflowTrigger>>()
            .AddSingleton<IEntitySerializer<WorkflowDefinition>, WorkflowDefinitionSerializer>()
            .AddSingleton<IEntitySerializer<WorkflowInstance>, WorkflowInstanceSerializer>()
            ;

        if (autoRunMigrations)
            services.AddHostedService<RunMigrations>();

        return services;
    }

    private static IServiceCollection AddEFCoreHandlers(this IServiceCollection services) => services.AddHandlersFrom<SaveWorkflowDefinitionHandler>();

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