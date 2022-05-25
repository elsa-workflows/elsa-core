using Elsa.Persistence.Common.Entities;
using Elsa.Persistence.EntityFrameworkCore.Common.HostedServices;
using Elsa.Persistence.EntityFrameworkCore.Common.Implementations;
using Elsa.Persistence.EntityFrameworkCore.Common.Services;
using Elsa.ServiceConfiguration.Abstractions;
using Elsa.ServiceConfiguration.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore.Common.Abstractions;

public abstract class EFCorePersistenceConfigurator<TDbContext> : ConfiguratorBase where TDbContext : DbContext
{
    public EFCorePersistenceConfigurator(IServiceConfiguration serviceConfiguration)
    {
        ServiceConfiguration = serviceConfiguration;
    }
 
    public IServiceConfiguration ServiceConfiguration { get; }
    public bool ContextPoolingIsEnabled { get; set; }
    public bool AutoRunMigrationsIsEnabled { get; set; } = true;
    public ServiceLifetime DbContextFactoryLifetime { get; set; } = ServiceLifetime.Singleton;
    public Action<IServiceProvider, DbContextOptionsBuilder> DbContextOptionsBuilderAction = (_, _) => { };

    public EFCorePersistenceConfigurator<TDbContext> WithContextPooling(bool enabled = true)
    {
        ContextPoolingIsEnabled = enabled;
        return this;
    }

    public EFCorePersistenceConfigurator<TDbContext> AutoRunMigrations(bool enabled = true)
    {
        AutoRunMigrationsIsEnabled = enabled;
        return this;
    }

    public EFCorePersistenceConfigurator<TDbContext> ConfigureDbContextOptions(Action<IServiceProvider, DbContextOptionsBuilder> configure)
    {
        DbContextOptionsBuilderAction = configure;
        return this;
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        if (ContextPoolingIsEnabled)
            services.AddPooledDbContextFactory<TDbContext>(DbContextOptionsBuilderAction);
        else
            services.AddDbContextFactory<TDbContext>(DbContextOptionsBuilderAction, DbContextFactoryLifetime);
    }

    public override void ConfigureHostedServices(IServiceCollection services)
    {
        if (AutoRunMigrationsIsEnabled)
            ServiceConfiguration.AddHostedService<RunMigrations<TDbContext>>(-1); // Migrations need to run before other hosted services that depend on DB access.
    }

    protected void AddStore<TEntity, TStore>(IServiceCollection services) where TEntity : Entity where TStore : class
    {
        services
            .AddSingleton<IStore<TDbContext, TEntity>, EFCoreStore<TDbContext, TEntity>>()
            .AddSingleton<TStore>();
    }
}