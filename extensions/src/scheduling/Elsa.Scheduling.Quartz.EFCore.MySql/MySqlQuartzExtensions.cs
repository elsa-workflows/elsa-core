using Elsa.Scheduling.Quartz.EFCore.MySql;
using Elsa.Scheduling.Quartz.Features;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

// ReSharper disable once CheckNamespace
namespace Elsa.Persistence.EFCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use SQL Server.
/// </summary>
[PublicAPI]
public static class MySqlQuartzExtensions
{
    /// <summary>
    /// Configures the <see cref="QuartzFeature"/> to use the MySQL job store.
    /// </summary>
    /// <param name="feature">The Quartz feature to configure.</param>
    /// <param name="connectionString">The MySQL connection string.</param>
    /// <param name="useClustering">Whether to enable Quartz clustering.</param>
    /// <param name="useContextPooling">Whether to use DbContext pooling.</param>
    /// <param name="configureDbContextOptions">An optional callback to further configure the <see cref="DbContextOptionsBuilder"/>, e.g. to apply a naming convention or set a custom migrations history table.</param>
    public static QuartzFeature UseMySql(this QuartzFeature feature, string connectionString = Constants.DefaultConnectionString, bool useClustering = true, bool useContextPooling = false, Action<DbContextOptionsBuilder>? configureDbContextOptions = null)
    {
        if (useContextPooling)
            feature.Services.AddPooledDbContextFactory<MySqlQuartzDbContext>(options => UseMySql(connectionString, options, configureDbContextOptions));
        else
            feature.Services.AddDbContextFactory<MySqlQuartzDbContext>(options => UseMySql(connectionString, options, configureDbContextOptions));

        feature.ConfigureQuartz += quartz =>
        {
            quartz.UsePersistentStore(store =>
            {
                store.UseNewtonsoftJsonSerializer();
                store.UseMySqlConnector(options => options.ConnectionString = connectionString);

                if (useClustering)
                    store.UseClustering();
            });
        };

        feature.Module.ConfigureHostedService<RunMigrationsHostedService<MySqlQuartzDbContext>>(-100);

        return feature;
    }

    private static void UseMySql(string connectionString, DbContextOptionsBuilder options, Action<DbContextOptionsBuilder>? configureDbContextOptions)
    {
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mySqlDbContextOptionsBuilder => { mySqlDbContextOptionsBuilder.MigrationsAssembly(typeof(MySqlQuartzDbContext).Assembly.GetName().Name); });

        configureDbContextOptions?.Invoke(options);
    }
}