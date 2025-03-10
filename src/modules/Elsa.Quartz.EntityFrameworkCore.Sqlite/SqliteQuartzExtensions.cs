using Elsa.Quartz.EntityFrameworkCore.Sqlite;
using Elsa.Quartz.Features;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use SQLite.
/// </summary>
[PublicAPI]
public static class SqliteQuartzExtensions
{
    /// <summary>
    /// Configures the <see cref="QuartzFeature"/> to use the SQLite job store.
    /// </summary>
    public static QuartzFeature UseSqlite(this QuartzFeature feature, string connectionString = Constants.DefaultConnectionString, bool useContextPooling = false)
    {
        if (useContextPooling)
            feature.Services.AddPooledDbContextFactory<SqliteQuartzDbContext>(options => UseSqlite(connectionString, options));
        else
            feature.Services.AddDbContextFactory<SqliteQuartzDbContext>(options => UseSqlite(connectionString, options));

        feature.ConfigureQuartz += quartz =>
        {
            quartz.UsePersistentStore(store =>
            {
                store.UseNewtonsoftJsonSerializer();
                store.UseMicrosoftSQLite(connectionString);
            });
        };

        feature.Module.ConfigureHostedService<RunMigrationsHostedService<SqliteQuartzDbContext>>(-100);

        return feature;
    }

    private static void UseSqlite(string connectionString, DbContextOptionsBuilder options)
    {
        // Use SQLite migrations.
        options.UseSqlite(connectionString, sqlite => { sqlite.MigrationsAssembly(typeof(SqliteQuartzDbContext).Assembly.GetName().Name); });
    }
}