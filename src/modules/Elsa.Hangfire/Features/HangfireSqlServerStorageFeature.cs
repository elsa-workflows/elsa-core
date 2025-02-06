using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Hangfire;
using Hangfire.SqlServer;

namespace Elsa.Hangfire.Features;

/// <summary>
/// Configures the Hangfire feature to use SQL Server storage. If you're setting up Hangfire yourself, then you should not enable this feature.
/// </summary>
[DependsOn(typeof(HangfireFeature))]
[Obsolete("Configure storage directly on the HangfireFeature.")]
public class HangfireSqlServerStorageFeature(IModule module) : FeatureBase(module)
{
    /// <summary>
    /// The connection string to use when connecting to SQL Server, or the name of the connection string.
    /// </summary>
    public string NameOrConnectionString { get; set; } = null!;

    /// <summary>
    /// Configures the SQL Server storage options.
    /// </summary>
    public Action<SqlServerStorageOptions> ConfigureSqlServerStorageOptions { get; set; } = _ => { };

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Use<HangfireFeature>(hangfireFeature =>
        {
            var storageOptions = new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.FromSeconds(15),
                UseRecommendedIsolationLevel = true
            };
            ConfigureSqlServerStorageOptions(storageOptions);

            hangfireFeature.ConfigureHangfire((_, cfg) =>
            {
                cfg.UseSqlServerStorage(NameOrConnectionString, storageOptions);
            });
        });
    }
}