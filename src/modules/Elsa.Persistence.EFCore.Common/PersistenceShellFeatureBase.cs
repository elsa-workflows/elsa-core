using CShells.Features;
using Elsa.Common.Entities;
using Elsa.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Persistence.EFCore;

public abstract class PersistenceShellFeatureBase<TFeature, TDbContext> : IShellFeature
    where TDbContext : ElsaDbContextBase
{
    /// <summary>
    /// Gets or sets a value indicating whether to use context pooling.
    /// </summary>
    public virtual bool UseContextPooling { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to run migrations.
    /// </summary>
    public virtual bool RunMigrations { get; set; } = true;

    /// <summary>
    /// Gets or sets the lifetime of the <see cref="IDbContextFactory{TContext}"/>. Defaults to <see cref="ServiceLifetime.Singleton"/>.
    /// </summary>
    public ServiceLifetime DbContextFactoryLifetime { get; set; } = ServiceLifetime.Scoped;

    /// <summary>
    /// Gets or sets the callback used to configure the <see cref="DbContextOptionsBuilder"/>.
    /// </summary>
    public virtual Action<IServiceProvider, DbContextOptionsBuilder> DbContextOptionsBuilder { get; set; } = null!;
    
    public void ConfigureServices(IServiceCollection services)
    {
        if (DbContextOptionsBuilder == null)
            throw new InvalidOperationException("The DbContextOptionsBuilder must be configured.");

        Action<IServiceProvider, DbContextOptionsBuilder> setup = (sp, opts) =>
        {
            opts.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            DbContextOptionsBuilder(sp, opts);
        };

        if (UseContextPooling)
            services.AddPooledDbContextFactory<TDbContext>(setup);
        else
            services.AddDbContextFactory<TDbContext>(setup, DbContextFactoryLifetime);

        services.Decorate<IDbContextFactory<TDbContext>, TenantAwareDbContextFactory<TDbContext>>();

        services.Configure<MigrationOptions>(options =>
        {
            options.RunMigrations[typeof(TDbContext)] = RunMigrations;
        });
        
        OnConfiguring(services);   
    }

    protected virtual void OnConfiguring(IServiceCollection services)
    {
    }

    protected virtual void ConfigureMigrations(IServiceCollection services)
    {
        services.AddStartupTask<RunMigrationsStartupTask<TDbContext>>();
    }

    /// <summary>
    /// Adds a store to the service collection.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <typeparam name="TStore">The type of the store.</typeparam>
    protected void AddStore<TEntity, TStore>(IServiceCollection services) where TEntity : class, new() where TStore : class
    {
        services
            .AddScoped<Store<TDbContext, TEntity>>()
            .AddScoped<TStore>()
            ;
    }

    /// <summary>
    /// Adds an entity store to the service collection.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <typeparam name="TStore">The type of the store.</typeparam>
    protected void AddEntityStore<TEntity, TStore>(IServiceCollection services) where TEntity : Entity, new() where TStore : class
    {
        services
            .AddScoped<EntityStore<TDbContext, TEntity>>()
            .AddScoped<TStore>()
            ;
    }
}