using Elsa.EntityFrameworkCore.Common;
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
    public static QuartzFeature UseSqlServer(this QuartzFeature feature, string connectionString = Constants.DefaultConnectionString, bool useClustering = true)
    {
        feature.Services.AddDbContextFactory<SqlServerQuartzDbContext>(options =>
        {
            // Use SQL Server migrations.
            options.UseSqlServer(connectionString, sqlServerDbContextOptionsBuilder => { sqlServerDbContextOptionsBuilder.MigrationsAssembly(typeof(SqlServerQuartzDbContext).Assembly.GetName().Name); });
        });

        feature.ConfigureQuartz += quartz =>
        {
            quartz.UsePersistentStore(store =>
            {
                store.UseNewtonsoftJsonSerializer();
                store.UseSqlServer(connectionString);
                
                if (useClustering)
                    store.UseClustering();
            });
        };

        // Configure the Quartz hosted service to run migrations.
        feature.Module.ConfigureHostedService<RunMigrationsHostedService<SqlServerQuartzDbContext>>(-100);

        return feature;
    }
}