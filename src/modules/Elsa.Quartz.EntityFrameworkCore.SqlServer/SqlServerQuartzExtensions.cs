using Elsa.Extensions;
using Elsa.Quartz.EntityFrameworkCore.SqlServer;
using Elsa.Quartz.Features;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use SQL Server.
/// </summary>
[PublicAPI]
public static class SqlServerQuartzExtensions
{
    /// <summary>
    /// Configures the <see cref="QuartzFeature"/> to use the SQL Server job store.
    /// </summary>
    public static QuartzFeature UseSqlServer(this QuartzFeature feature, string connectionString = Constants.DefaultConnectionString, bool useClustering = true, bool useContextPooling = false)
    {
        if (useContextPooling)
            feature.Services.AddPooledDbContextFactory<SqlServerQuartzDbContext>(options => UseSqlServer(connectionString, options));
        else
            feature.Services.AddDbContextFactory<SqlServerQuartzDbContext>(options => UseSqlServer(connectionString, options));

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

        feature.Services.AddStartupTask<RunMigrationsStartupTask<SqlServerQuartzDbContext>>();

        return feature;
    }

    private static void UseSqlServer(string connectionString, DbContextOptionsBuilder options)
    {
        // Use SQL Server migrations.
        options.UseSqlServer(connectionString, sqlServerDbContextOptionsBuilder => { sqlServerDbContextOptionsBuilder.MigrationsAssembly(typeof(SqlServerQuartzDbContext).Assembly.GetName().Name); });
    }
}