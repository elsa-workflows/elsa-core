using Elsa.Common.Entities;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Common;

/// <summary>
/// Base class for features that require Entity Framework Core.
/// </summary>
/// <typeparam name="TDbContext">The type of the database context.</typeparam>
public abstract class PersistenceFeatureBase<TDbContext> : FeatureBase where TDbContext : DbContext
{
    /// <inheritdoc />
    protected PersistenceFeatureBase(IModule module) : base(module)
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use context pooling.
    /// </summary>
    public bool UseContextPooling { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether to run migrations.
    /// </summary>
    public bool RunMigrations { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the lifetime of the <see cref="IDbContextFactory{TContext}"/>. Defaults to <see cref="ServiceLifetime.Singleton"/>.
    /// </summary>
    public ServiceLifetime DbContextFactoryLifetime { get; set; } = ServiceLifetime.Singleton;

    /// <summary>
    /// Gets or sets the callback used to configure the <see cref="DbContextOptionsBuilder"/>.
    /// </summary>
    public Action<IServiceProvider, DbContextOptionsBuilder> DbContextOptionsBuilder = (_, options) => options
        .UseSqlite("Data Source=elsa.sqlite.db;Cache=Shared;", sqlite => sqlite
            .MigrationsAssembly("Elsa.EntityFrameworkCore.Sqlite")
            .MigrationsHistoryTable(ElsaDbContextBase.MigrationsHistoryTable, ElsaDbContextBase.ElsaSchema));

    /// <inheritdoc />
    public override void ConfigureHostedServices()
    {
        if (RunMigrations)
            Module.ConfigureHostedService<RunMigrationsHostedService<TDbContext>>(-100); // Migrations need to run before other hosted services that depend on DB access.
    }

    /// <inheritdoc />
    public override void Apply()
    {
        if (UseContextPooling)
            Services.AddPooledDbContextFactory<TDbContext>(DbContextOptionsBuilder);
        else
            Services.AddDbContextFactory<TDbContext>(DbContextOptionsBuilder, DbContextFactoryLifetime);
    }

    /// <summary>
    /// Adds a store to the service collection.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <typeparam name="TStore">The type of the store.</typeparam>
    protected void AddStore<TEntity, TStore>() where TEntity : class where TStore : class
    {
        Services
            .AddSingleton<Store<TDbContext, TEntity>>()
            .AddSingleton<TStore>()
            ;
    }

    /// <summary>
    /// Adds an entity store to the service collection.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <typeparam name="TStore">The type of the store.</typeparam>
    protected void AddEntityStore<TEntity, TStore>() where TEntity : Entity where TStore : class
    {
        Services
            .AddSingleton<EntityStore<TDbContext, TEntity>>()
            .AddSingleton<TStore>()
            ;
    }
}