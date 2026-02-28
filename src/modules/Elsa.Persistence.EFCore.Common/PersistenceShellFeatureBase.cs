using System.Reflection;
using CShells.Features;
using Elsa.Common.Entities;
using Elsa.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Elsa.Persistence.EFCore;

public abstract class PersistenceShellFeatureBase<TDbContext> : IShellFeature
    where TDbContext : DbContext
{
    /// <summary>
    /// Gets or sets a value indicating whether to use context pooling.
    /// When not explicitly set, falls back to shared settings if available.
    /// </summary>
    public bool? UseContextPooling { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to run migrations.
    /// When not explicitly set, falls back to shared settings if available, defaulting to true.
    /// </summary>
    public bool? RunMigrations { get; set; }

    /// <summary>
    /// Gets or sets the lifetime of the <see cref="IDbContextFactory{TContext}"/>.
    /// When not explicitly set, falls back to shared settings if available, defaulting to <see cref="ServiceLifetime.Scoped"/>.
    /// </summary>
    public ServiceLifetime? DbContextFactoryLifetime { get; set; }

    /// <summary>
    /// Gets or sets the connection string to use for the database.
    /// When not explicitly set, falls back to shared settings if available.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets additional options to configure the database context.
    /// When not explicitly set, falls back to shared settings if available.
    /// </summary>
    public ElsaDbContextOptions? DbContextOptions { get; set; }

    /// <summary>
    /// Gets or sets the callback used to configure the <see cref="DbContextOptionsBuilder"/>.
    /// </summary>
    protected virtual Action<IServiceProvider, DbContextOptionsBuilder> DbContextOptionsBuilder { get; set; } = (_, _) => { };
    
    public void ConfigureServices(IServiceCollection services)
    {
        // Capture feature-specific settings
        var featureConnectionString = ConnectionString;
        var featureDbContextOptions = DbContextOptions;
        var featureUseContextPooling = UseContextPooling;
        var featureRunMigrations = RunMigrations;
        var featureDbContextFactoryLifetime = DbContextFactoryLifetime;

        // Resolve effective settings at runtime, falling back to shared settings
        Action<IServiceProvider, DbContextOptionsBuilder> setup = (sp, opts) =>
        {
            var sharedSettings = sp.GetService<IOptions<SharedPersistenceSettings>>()?.Value;
            
            var connectionString = featureConnectionString 
                ?? sharedSettings?.ConnectionString
                ?? throw new InvalidOperationException(
                    $"Connection string not configured for {GetType().Name}. " +
                    $"Either configure the feature directly or provide shared settings via the combined persistence feature.");
            
            var dbContextOptions = featureDbContextOptions ?? sharedSettings?.DbContextOptions;

            opts.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

            // Configure the database provider
            var migrationsAssembly = GetMigrationsAssembly();
            ConfigureProvider(opts, migrationsAssembly, connectionString, dbContextOptions);

            // Allow derived classes to further configure
            DbContextOptionsBuilder(sp, opts);
        };

        // Resolve pooling and lifetime settings with fallback
        // Note: These are resolved at configuration time, not runtime, but they'll use defaults if not set
        var useContextPooling = featureUseContextPooling ?? false;
        var dbContextFactoryLifetime = featureDbContextFactoryLifetime ?? ServiceLifetime.Scoped;
        var runMigrations = featureRunMigrations ?? true;

        if (useContextPooling)
            services.AddPooledDbContextFactory<TDbContext>(setup);
        else
            services.AddDbContextFactory<TDbContext>(setup, dbContextFactoryLifetime);

        services.Decorate<IDbContextFactory<TDbContext>, TenantAwareDbContextFactory<TDbContext>>();

        services.Configure<MigrationOptions>(options =>
        {
            options.RunMigrations[typeof(TDbContext)] = runMigrations;
        });

        services.AddStartupTask<RunMigrationsStartupTask<TDbContext>>();
        OnConfiguring(services);
    }

    /// <summary>
    /// Gets the assembly containing migrations for this provider.
    /// By default, returns the assembly of the concrete feature type.
    /// </summary>
    protected virtual Assembly GetMigrationsAssembly() => GetType().Assembly;

    /// <summary>
    /// Configures the database provider for the specified <see cref="DbContextOptionsBuilder"/>.
    /// </summary>
    /// <param name="builder">The options builder to configure.</param>
    /// <param name="migrationsAssembly">The assembly containing migrations.</param>
    /// <param name="connectionString">The connection string to use.</param>
    /// <param name="options">Additional options to configure the database context.</param>
    protected abstract void ConfigureProvider(
        DbContextOptionsBuilder builder,
        Assembly migrationsAssembly,
        string connectionString,
        ElsaDbContextOptions? options);

    protected virtual void OnConfiguring(IServiceCollection services)
    {
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