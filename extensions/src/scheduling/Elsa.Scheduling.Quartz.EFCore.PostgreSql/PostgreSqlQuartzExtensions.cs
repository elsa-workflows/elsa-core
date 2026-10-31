using Elsa.Scheduling.Quartz.EFCore.PostgreSql;
using Elsa.Scheduling.Quartz.Features;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

// ReSharper disable once CheckNamespace
namespace Elsa.Persistence.EFCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use PostgreSQL.
/// </summary>
[PublicAPI]
public static class PostgreSqlQuartzExtensions
{
    /// <summary>
    /// Configures the <see cref="QuartzFeature"/> to use the PostgreSQL job store.
    /// </summary>
    /// <param name="feature">The Quartz feature to configure.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <param name="useClustering">Whether to enable Quartz clustering.</param>
    /// <param name="useContextPooling">Whether to use DbContext pooling.</param>
    /// <param name="configureDbContextOptions">An optional callback to further configure the <see cref="DbContextOptionsBuilder"/>, e.g. to apply a naming convention or set a custom migrations history table.</param>
    public static QuartzFeature UsePostgreSql(this QuartzFeature feature, string connectionString = Constants.DefaultConnectionString, bool useClustering = true, bool useContextPooling = false, Action<DbContextOptionsBuilder>? configureDbContextOptions = null)
    {
        if (useContextPooling)
            feature.Services.AddPooledDbContextFactory<PostgreSqlQuartzDbContext>(options => UseNpgsql(connectionString, options, configureDbContextOptions));
        else
            feature.Services.AddDbContextFactory<PostgreSqlQuartzDbContext>(options => UseNpgsql(connectionString, options, configureDbContextOptions));

        feature.ConfigureQuartz += quartz =>
        {
            quartz.UsePersistentStore(store =>
            {
                store.UseNewtonsoftJsonSerializer();
                store.UsePostgres(options =>
                {
                    options.ConnectionString = connectionString;
                    options.TablePrefix = "quartz.qrtz_";

                });

                if (useClustering)
                    store.UseClustering();
            });
        };

        feature.Module.ConfigureHostedService<RunMigrationsHostedService<PostgreSqlQuartzDbContext>>(-100);

        return feature;
    }

    private static void UseNpgsql(string connectionString, DbContextOptionsBuilder options, Action<DbContextOptionsBuilder>? configureDbContextOptions)
    {
        // Use PostgreSQL migrations.
        options.UseNpgsql(connectionString, sqlServerDbContextOptionsBuilder => { sqlServerDbContextOptionsBuilder.MigrationsAssembly(typeof(PostgreSqlQuartzDbContext).Assembly.GetName().Name); });

        configureDbContextOptions?.Invoke(options);
    }
}