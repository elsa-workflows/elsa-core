using Elsa.Scheduling.Quartz.EFCore.Sqlite;
using Elsa.Scheduling.Quartz.Features;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

// ReSharper disable once CheckNamespace
namespace Elsa.Persistence.EFCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use SQLite.
/// </summary>
[PublicAPI]
public static class SqliteQuartzExtensions
{
    /// <summary>
    /// Configures the <see cref="QuartzFeature"/> to use the SQLite job store.
    /// </summary>
    /// <param name="feature">The Quartz feature to configure.</param>
    /// <param name="connectionString">The SQLite connection string.</param>
    /// <param name="useContextPooling">Whether to use DbContext pooling.</param>
    /// <param name="useClustering">Whether to enable Quartz clustering.</param>
    /// <param name="configureDbContextOptions">An optional callback to further configure the <see cref="DbContextOptionsBuilder"/>, e.g. to apply a naming convention or set a custom migrations history table.</param>
    public static QuartzFeature UseSqlite(this QuartzFeature feature, string connectionString = Constants.DefaultConnectionString, bool useContextPooling = false, bool useClustering = false, Action<DbContextOptionsBuilder>? configureDbContextOptions = null)
    {
        if (useContextPooling)
            feature.Services.AddPooledDbContextFactory<SqliteQuartzDbContext>(options => UseSqlite(connectionString, options, configureDbContextOptions));
        else
            feature.Services.AddDbContextFactory<SqliteQuartzDbContext>(options => UseSqlite(connectionString, options, configureDbContextOptions));

        feature.ConfigureQuartz += quartz =>
        {
            quartz.UsePersistentStore(store =>
            {
                store.UseNewtonsoftJsonSerializer();
                store.UseMicrosoftSQLite(connectionString);
                
                if (useClustering)
                    store.UseClustering();
            });
        };

        feature.Module.ConfigureHostedService<RunMigrationsHostedService<SqliteQuartzDbContext>>(-100);

        return feature;
    }

    private static void UseSqlite(string connectionString, DbContextOptionsBuilder options, Action<DbContextOptionsBuilder>? configureDbContextOptions)
    {
        // Use SQLite migrations.
        options.UseSqlite(connectionString, sqlite => { sqlite.MigrationsAssembly(typeof(SqliteQuartzDbContext).Assembly.GetName().Name); });

        configureDbContextOptions?.Invoke(options);
    }
}