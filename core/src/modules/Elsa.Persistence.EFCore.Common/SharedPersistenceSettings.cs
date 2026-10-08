using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Persistence.EFCore;

/// <summary>
/// Represents shared persistence settings that can be cascaded to dependent persistence features.
/// When a combined persistence feature (e.g., SqliteWorkflowPersistence) is configured,
/// it can register these shared settings that individual features will use as defaults.
/// </summary>
public class SharedPersistenceSettings
{
    /// <summary>
    /// Gets or sets the connection string to use for the database.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets additional options to configure the database context.
    /// </summary>
    public ElsaDbContextOptions? DbContextOptions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use context pooling.
    /// </summary>
    public bool? UseContextPooling { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to run migrations.
    /// </summary>
    public bool? RunMigrations { get; set; }

    /// <summary>
    /// Gets or sets the lifetime of the DbContextFactory.
    /// </summary>
    public ServiceLifetime? DbContextFactoryLifetime { get; set; }
}

