using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Common;

public abstract class PersistenceFeatureBase<TDbContext> : FeatureBase where TDbContext : DbContext
{
    protected PersistenceFeatureBase(IModule module) : base(module)
    {
    }

    public bool UseContextPooling { get; set; }
    public bool RunMigrations { get; set; } = true;
    public ServiceLifetime DbContextFactoryLifetime { get; set; } = ServiceLifetime.Singleton;
    public Action<IServiceProvider, DbContextOptionsBuilder> DbContextOptionsBuilder = (_, _) => { };

    public override void ConfigureHostedServices()
    {
        if (RunMigrations)
            Module.ConfigureHostedService<RunMigrationsHostedService<TDbContext>>(-100); // Migrations need to run before other hosted services that depend on DB access.
    }

    public override void Apply()
    {
        if (UseContextPooling)
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