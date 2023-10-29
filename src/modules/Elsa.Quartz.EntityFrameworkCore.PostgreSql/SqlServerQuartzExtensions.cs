using Elsa.EntityFrameworkCore.Common;
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
    public static QuartzFeature UseSqlServer(this QuartzFeature feature, string connectionString = Constants.DefaultConnectionString, bool useClustering = true)
    {
        feature.Services.AddDbContextFactory<PostgreSqlQuartzDbContext>(options =>
        {
            // Use PostgreSQL migrations.
            options.UseNpgsql(connectionString, sqlServerDbContextOptionsBuilder => { sqlServerDbContextOptionsBuilder.MigrationsAssembly(typeof(PostgreSqlQuartzDbContext).Assembly.GetName().Name); });
        });

        feature.ConfigureQuartz += quartz =>
        {
            quartz.UsePersistentStore(store =>
            {
                store.UseNewtonsoftJsonSerializer();
                store.UsePostgres(connectionString);
                
                if (useClustering)
                    store.UseClustering();
            });
        };

        // Configure the Quartz hosted service to run migrations.
        feature.Module.ConfigureHostedService<RunMigrationsHostedService<PostgreSqlQuartzDbContext>>(-100);

        return feature;
    }
}