using CShells.Features;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Persistence.EFCore;

/// <summary>
/// Base class for combined persistence shell features that provide unified configuration
/// for multiple persistence components (definitions, instances, runtime, etc.).
/// 
/// This base class handles registering <see cref="SharedPersistenceSettings"/> so that
/// dependent persistence features can use the shared settings as a fallback when
/// feature-specific settings are not provided.
/// </summary>
/// <remarks>
/// <para>
/// By using this base class, users can configure persistence settings once at the
/// combined feature level instead of repeating configuration for each dependent feature.
/// </para>
/// <example>
/// Example appsettings.json with unified configuration:
/// <code>
/// {
///   "CShells": {
///     "Shells": [{
///       "Settings": {
///         "SqliteWorkflowPersistence": {
///           "ConnectionString": "Data Source=elsa.db;Cache=Shared",
///           "DbContextOptions": {
///             "EnableSensitiveDataLogging": false
///           }
///         }
///       },
///       "Features": ["SqliteWorkflowPersistence"]
///     }]
///   }
/// }
/// </code>
/// </example>
/// </remarks>
public abstract class CombinedPersistenceShellFeatureBase : IShellFeature
{
    /// <summary>
    /// Gets or sets the connection string to use for the database.
    /// This setting is shared with all dependent persistence features.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets additional options to configure the database context.
    /// This setting is shared with all dependent persistence features.
    /// </summary>
    public ElsaDbContextOptions? DbContextOptions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use context pooling.
    /// This setting is shared with all dependent persistence features.
    /// </summary>
    public bool? UseContextPooling { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to run migrations.
    /// This setting is shared with all dependent persistence features.
    /// </summary>
    public bool? RunMigrations { get; set; }

    /// <summary>
    /// Gets or sets the lifetime of the DbContextFactory.
    /// This setting is shared with all dependent persistence features.
    /// </summary>
    public ServiceLifetime? DbContextFactoryLifetime { get; set; }

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        // Register shared settings that dependent features will use as fallback
        services.Configure<SharedPersistenceSettings>(options =>
        {
            // Only set values that were explicitly configured
            if (ConnectionString != null)
                options.ConnectionString = ConnectionString;
            
            if (DbContextOptions != null)
                options.DbContextOptions = DbContextOptions;
            
            if (UseContextPooling.HasValue)
                options.UseContextPooling = UseContextPooling;
            
            if (RunMigrations.HasValue)
                options.RunMigrations = RunMigrations;
            
            if (DbContextFactoryLifetime.HasValue)
                options.DbContextFactoryLifetime = DbContextFactoryLifetime;
        });

        OnConfiguring(services);
    }

    /// <summary>
    /// Override this method to add additional service registrations.
    /// </summary>
    protected virtual void OnConfiguring(IServiceCollection services)
    {
    }
}

