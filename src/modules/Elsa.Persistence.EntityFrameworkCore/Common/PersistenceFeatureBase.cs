using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore.Common;

public abstract class PersistenceFeatureBase<TDbContext> : FeatureBase where TDbContext : DbContext
{
    protected PersistenceFeatureBase(IModule module) : base(module)
    {
    }

    public bool ContextPoolingIsEnabled { get; set; }
    public bool AutoRunMigrationsIsEnabled { get; set; } = true;
    public ServiceLifetime DbContextFactoryLifetime { get; set; } = ServiceLifetime.Singleton;
    public Action<IServiceProvider, DbContextOptionsBuilder> DbContextOptionsBuilder = (_, _) => { };

    public override void ConfigureHostedServices()
    {
        if (AutoRunMigrationsIsEnabled)
            Module.ConfigureHostedService<RunMigrationsHostedService<TDbContext>>(-1); // Migrations need to run before other hosted services that depend on DB access.
    }

    public override void Apply()
    {
        if (ContextPoolingIsEnabled)
            Services.AddPooledDbContextFactory<TDbContext>(DbContextOptionsBuilder);
        else
            Services.AddDbContextFactory<TDbContext>(DbContextOptionsBuilder, DbContextFactoryLifetime);
    }

    protected void AddStore<TEntity, TStore>() where TEntity : class where TStore : class
    {
        Services
            .AddSingleton<Store<TDbContext, TEntity>>()
            .AddSingleton<TStore>()
            ;
    }
}