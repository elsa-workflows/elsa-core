using CShells.Features;
using CShells.Lifecycle;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Elsa.Scheduling.Quartz.EFCore.PostgreSql;
using Elsa.Scheduling.Quartz.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Elsa.Scheduling.Quartz.EFCore.PostgreSql.ShellFeatures;

/// <summary>
/// Configures Quartz to use PostgreSQL as the persistent job store via EF Core.
/// </summary>
[ShellFeature(
    DisplayName = "Quartz PostgreSQL Store",
    Description = "Configures Quartz.NET to persist jobs and triggers in PostgreSQL",
    DependsOn = [typeof(QuartzSchedulerFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("postgresql-database", "database", Reason = "Stores Quartz scheduler data in PostgreSQL.", Providers = new[] { "PostgreSQL" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class QuartzPostgreSqlFeature : IShellFeature
{
    /// <summary>The PostgreSQL connection string.</summary>
    [ManifestSetting(
        DisplayName = "Connection string",
        Description = "The PostgreSQL connection string used by the Quartz persistent job store.",
        Category = "Persistence",
        Secret = true,
        Required = true,
        HasRequired = true,
        RestartRequired = true)]
    public string ConnectionString { get; set; } = "Host=localhost;Database=quartz;Username=postgres;Password=postgres";

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
            services.AddPooledDbContextFactory<PostgreSqlQuartzDbContext>(Configure);
        else
            services.AddDbContextFactory<PostgreSqlQuartzDbContext>(Configure);

        services.AddShellInitializer<EfCoreMigrationHandler>(LifecyclePhase.Prepare, order: 100);

        services.AddQuartz(quartz =>
        {
            quartz.UsePersistentStore(store =>
            {
                store.UseNewtonsoftJsonSerializer();
                store.UsePostgres(options =>
                {
                    options.ConnectionString = ConnectionString;
                    options.TablePrefix = "quartz.qrtz_";
                });

                if (UseClustering)
                    store.UseClustering();
            });
        });
    }

    private void Configure(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(ConnectionString, npgsql =>
            npgsql.MigrationsAssembly(typeof(PostgreSqlQuartzDbContext).Assembly.GetName().Name));
        ConfigureDbContextOptions?.Invoke(options);
    }
}

/// Runs EF Core migrations before the Quartz scheduler starts on shell activation.
file sealed class EfCoreMigrationHandler(IDbContextFactory<PostgreSqlQuartzDbContext> factory) : IShellInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
    }
}
