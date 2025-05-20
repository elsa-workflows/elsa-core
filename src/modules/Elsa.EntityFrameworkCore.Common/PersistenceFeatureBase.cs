using Elsa.Common.Entities;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore;

/// <summary>
/// Base class for features that require Entity Framework Core.
/// </summary>
/// <typeparam name="TDbContext">The type of the database context.</typeparam>
/// <typeparam name="TFeature">The type of the feature.</typeparam>
[DependsOn(typeof(CommonPersistenceFeature))]
public abstract class PersistenceFeatureBase<TFeature, TDbContext>(IModule module) : FeatureBase(module)
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

    public override void ConfigureHostedServices()
    {
        if (RunMigrations)
            ConfigureMigrations();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        if (DbContextOptionsBuilder == null)
            throw new InvalidOperationException("The DbContextOptionsBuilder must be configured.");

        Action<IServiceProvider, DbContextOptionsBuilder> setup = (sp, opts) =>
        {
            opts.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            DbContextOptionsBuilder(sp, opts);
        };

        if (UseContextPooling)
            Services.AddPooledDbContextFactory<TDbContext>(setup);
        else
            Services.AddDbContextFactory<TDbContext>(setup, DbContextFactoryLifetime);

        Services.Decorate<IDbContextFactory<TDbContext>, TenantAwareDbContextFactory<TDbContext>>();
    }

    protected virtual void ConfigureMigrations()
    {
        Services.AddStartupTask<RunMigrationsStartupTask<TDbContext>>();
    }

    /// <summary>
    /// Adds a store to the service collection.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <typeparam name="TStore">The type of the store.</typeparam>
    protected void AddStore<TEntity, TStore>() where TEntity : class, new() where TStore : class
    {
        Services
            .AddScoped<Store<TDbContext, TEntity>>()
            .AddScoped<TStore>()
            ;
    }

    /// <summary>
    /// Adds an entity store to the service collection.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <typeparam name="TStore">The type of the store.</typeparam>
    protected void AddEntityStore<TEntity, TStore>() where TEntity : Entity, new() where TStore : class
    {
        Services
            .AddScoped<EntityStore<TDbContext, TEntity>>()
            .AddScoped<TStore>()
            ;
    }
}