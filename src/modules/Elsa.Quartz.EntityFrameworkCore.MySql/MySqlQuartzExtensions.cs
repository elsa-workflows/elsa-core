using Elsa.EntityFrameworkCore.Common;
using Elsa.Quartz.EntityFrameworkCore.MySql;
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
public static class MySqlQuartzExtensions
{
    /// <summary>
    /// Configures the <see cref="QuartzFeature"/> to use the MySQL job store.
    /// </summary>
    public static QuartzFeature UseMySql(this QuartzFeature feature, string connectionString = Constants.DefaultConnectionString, bool useClustering = true)
    {
        feature.Services.AddDbContextFactory<MySqlQuartzDbContext>(options =>
        {
            // Use MySQL migrations.
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), sqlServerDbContextOptionsBuilder => { sqlServerDbContextOptionsBuilder.MigrationsAssembly(typeof(MySqlQuartzDbContext).Assembly.GetName().Name); });
        });

        feature.ConfigureQuartz += quartz =>
        {
            quartz.UsePersistentStore(store =>
            {
                store.UseNewtonsoftJsonSerializer();
                store.UseMySql(connectionString);
                
                if (useClustering)
                    store.UseClustering();
            });
        };

        // Configure the Quartz hosted service to run migrations.
        feature.Module.ConfigureHostedService<RunMigrationsHostedService<MySqlQuartzDbContext>>(-100);

        return feature;
    }
}