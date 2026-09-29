using CShells.Features;
using CShells.Lifecycle;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Elsa.Scheduling.Quartz.EFCore.Sqlite;
using Elsa.Scheduling.Quartz.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Elsa.Scheduling.Quartz.EFCore.Sqlite.ShellFeatures;

/// <summary>
/// Configures Quartz to use SQLite as the persistent job store via EF Core.
/// </summary>
[ShellFeature(
    DisplayName = "Quartz SQLite Store",
    Description = "Configures Quartz.NET to persist jobs and triggers in SQLite",
    DependsOn = [typeof(QuartzSchedulerFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("sqlite-database", "database", Reason = "Stores Quartz scheduler data in SQLite.", Providers = new[] { "SQLite" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class QuartzSqliteFeature : IShellFeature, IPostConfigureShellServices
{
    /// <summary>The SQLite connection string.</summary>
    [ManifestSetting(
        DisplayName = "Connection string",
        Description = "The SQLite connection string used by the Quartz persistent job store.",
        Category = "Persistence",
        Secret = true,
        Required = true,
        HasRequired = true,
        RestartRequired = true)]
    public string ConnectionString { get; set; } = "Data Source=quartz.db";

    /// <summary>
    /// Enable Quartz clustering. Defaults to <c>false</c> — SQLite does not
    /// support true multi-node clustering.
    /// </summary>
    [ManifestSetting(
        DisplayName = "Use clustering",
        Description = "Enable Quartz clustering. SQLite does not support true multi-node clustering.",
        Category = "Persistence",
        Advanced = true,
        RestartRequired = true)]
    public bool UseClustering { get; set; }

    /// <summary>Use a pooled <c>IDbContextFactory</c>. Defaults to <c>false</c>.</summary>
    [ManifestSetting(
        DisplayName = "Use context pooling",
        Description = "Use a pooled EF Core IDbContextFactory.",
        Category = "Persistence",
        Advanced = true,
        RestartRequired = true)]
    public bool UseContextPooling { get; set; }

    /// <summary>An optional callback to configure the <see cref="DbContextOptionsBuilder"/>.</summary>
    public Action<DbContextOptionsBuilder>? ConfigureDbContextOptions { get; set; }

    public void ConfigureServices(IServiceCollection services)
    {
        if (UseContextPooling)
            services.AddPooledDbContextFactory<SqliteQuartzDbContext>(Configure);
        else
            services.AddDbContextFactory<SqliteQuartzDbContext>(Configure);

        services.AddShellInitializer<EfCoreMigrationHandler>(LifecyclePhase.Prepare, order: 100);
    }

    public void PostConfigureServices(IServiceCollection services)
    {
        services.AddQuartz(quartz =>
        {
            quartz.UsePersistentStore(store =>
            {
                store.UseNewtonsoftJsonSerializer();
                store.UseMicrosoftSQLite(ConnectionString);

                if (UseClustering)
                    store.UseClustering();
            });
        });
    }

    private void Configure(DbContextOptionsBuilder options)
    {
        options.UseSqlite(ConnectionString, sqlite =>
            sqlite.MigrationsAssembly(typeof(SqliteQuartzDbContext).Assembly.GetName().Name));
        ConfigureDbContextOptions?.Invoke(options);
    }
}

/// Runs EF Core migrations before the Quartz scheduler starts on shell activation.
file sealed class EfCoreMigrationHandler(IDbContextFactory<SqliteQuartzDbContext> factory) : IShellInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
    }
}
