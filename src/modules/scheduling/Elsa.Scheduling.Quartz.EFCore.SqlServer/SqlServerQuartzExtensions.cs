using Elsa.Scheduling.Quartz.EFCore.SqlServer;
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
public static class SqlServerQuartzExtensions
{
    /// <summary>
    /// Configures the <see cref="QuartzFeature"/> to use the SQL Server job store.
    /// </summary>
    /// <param name="feature">The Quartz feature to configure.</param>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <param name="useClustering">Whether to enable Quartz clustering.</param>
    /// <param name="useContextPooling">Whether to use DbContext pooling.</param>
    /// <param name="configureDbContextOptions">An optional callback to further configure the <see cref="DbContextOptionsBuilder"/>, e.g. to apply a naming convention or set a custom migrations history table.</param>
    public static QuartzFeature UseSqlServer(this QuartzFeature feature, string connectionString = Constants.DefaultConnectionString, bool useClustering = true, bool useContextPooling = false, Action<DbContextOptionsBuilder>? configureDbContextOptions = null)
    {
        if (useContextPooling)
            feature.Services.AddPooledDbContextFactory<SqlServerQuartzDbContext>(options => UseSqlServer(connectionString, options, configureDbContextOptions));
        else
            feature.Services.AddDbContextFactory<SqlServerQuartzDbContext>(options => UseSqlServer(connectionString, options, configureDbContextOptions));

        feature.ConfigureQuartz += quartz =>
        {
            quartz.UsePersistentStore(store =>
            {
                store.UseNewtonsoftJsonSerializer();
                store.UseSqlServer(options =>
                {
                    options.ConnectionString = connectionString;
                    options.TablePrefix = "[quartz].qrtz_";
                });

                if (useClustering)
                    store.UseClustering();
            });
        };

        feature.Module.ConfigureHostedService<RunMigrationsHostedService<SqlServerQuartzDbContext>>(-100);

        return feature;
    }

    private static void UseSqlServer(string connectionString, DbContextOptionsBuilder options, Action<DbContextOptionsBuilder>? configureDbContextOptions)
    {
        // Use SQL Server migrations.
        options.UseSqlServer(connectionString, sqlServerDbContextOptionsBuilder => { sqlServerDbContextOptionsBuilder.MigrationsAssembly(typeof(SqlServerQuartzDbContext).Assembly.GetName().Name); });

        configureDbContextOptions?.Invoke(options);
    }
}