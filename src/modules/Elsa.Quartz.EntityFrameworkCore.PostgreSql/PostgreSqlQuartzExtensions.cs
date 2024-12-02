using Elsa.Quartz.EntityFrameworkCore.PostgreSql;
using Elsa.Quartz.Features;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use PostgreSQL.
/// </summary>
[PublicAPI]
public static class PostgreSqlQuartzExtensions
{
    /// <summary>
    /// Configures the <see cref="QuartzFeature"/> to use the PostgreSQL job store.
    /// </summary>
    public static QuartzFeature UsePostgreSql(this QuartzFeature feature, string connectionString = Constants.DefaultConnectionString, bool useClustering = true, bool useContextPooling = false)
    {
        if (useContextPooling)
            feature.Services.AddPooledDbContextFactory<PostgreSqlQuartzDbContext>(options => UseNpgsql(connectionString, options));
        else
            feature.Services.AddDbContextFactory<PostgreSqlQuartzDbContext>(options => UseNpgsql(connectionString, options));

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

    private static void UseNpgsql(string connectionString, DbContextOptionsBuilder options)
    {
        // Use PostgreSQL migrations.
        options.UseNpgsql(connectionString, sqlServerDbContextOptionsBuilder => { sqlServerDbContextOptionsBuilder.MigrationsAssembly(typeof(PostgreSqlQuartzDbContext).Assembly.GetName().Name); });
    }
}