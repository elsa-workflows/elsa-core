using CShells.Features;
using CShells.Lifecycle;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Elsa.Scheduling.Quartz.EFCore.SqlServer;
using Elsa.Scheduling.Quartz.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Elsa.Scheduling.Quartz.EFCore.SqlServer.ShellFeatures;

/// <summary>
/// Configures Quartz to use SQL Server as the persistent job store via EF Core.
/// </summary>
[ShellFeature(
    DisplayName = "Quartz SQL Server Store",
    Description = "Configures Quartz.NET to persist jobs and triggers in SQL Server",
    DependsOn = [typeof(QuartzSchedulerFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("sqlserver-database", "database", Reason = "Stores Quartz scheduler data in SQL Server.", Providers = new[] { "SQL Server" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class QuartzSqlServerFeature : IShellFeature
{
    /// <summary>The SQL Server connection string.</summary>
    [ManifestSetting(
        DisplayName = "Connection string",
        Description = "The SQL Server connection string used by the Quartz persistent job store.",
        Category = "Persistence",
        Secret = true,
        Required = true,
        HasRequired = true,
        RestartRequired = true)]
    public string ConnectionString { get; set; } = "Server=localhost;Database=Quartz;Trusted_Connection=True;";

    /// <summary>Enable Quartz clustering. Defaults to <c>true</c>.</summary>
    [ManifestSetting(
        DisplayName = "Use clustering",
        Description = "Enable Quartz clustering.",
        Category = "Persistence",
        RestartRequired = true)]
    public bool UseClustering { get; set; } = true;

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
            services.AddPooledDbContextFactory<SqlServerQuartzDbContext>(Configure);
        else
            services.AddDbContextFactory<SqlServerQuartzDbContext>(Configure);

        services.AddShellInitializer<EfCoreMigrationHandler>(LifecyclePhase.Prepare, order: 100);

        // AddQuartz is additive — layers the persistent store onto the base
        // AddQuartz call already made by QuartzFeature (which ran first via DependsOn).
        services.AddQuartz(quartz =>
        {
            quartz.UsePersistentStore(store =>
            {
                store.UseNewtonsoftJsonSerializer();
                store.UseSqlServer(options =>
                {
                    options.ConnectionString = ConnectionString;
                    options.TablePrefix = "[quartz].qrtz_";
                });

                if (UseClustering)
                    store.UseClustering();
            });
        });
    }

    private void Configure(DbContextOptionsBuilder options)
    {
        options.UseSqlServer(ConnectionString, sql =>
            sql.MigrationsAssembly(typeof(SqlServerQuartzDbContext).Assembly.GetName().Name));
        ConfigureDbContextOptions?.Invoke(options);
    }
}

/// Runs EF Core migrations before the Quartz scheduler starts on shell activation.
file sealed class EfCoreMigrationHandler(IDbContextFactory<SqlServerQuartzDbContext> factory) : IShellInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
    }
}
