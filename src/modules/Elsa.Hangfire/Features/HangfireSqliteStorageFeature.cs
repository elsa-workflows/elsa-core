using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Hangfire.Storage.SQLite;

namespace Elsa.Hangfire.Features;

/// <summary>
/// Configures the Hangfire feature to use SQLite storage. If you're setting up Hangfire yourself, then you should not enable this feature.
/// </summary>
[DependsOn(typeof(HangfireFeature))]
public class HangfireSqliteStorageFeature : FeatureBase
{
    /// <inheritdoc />
    public HangfireSqliteStorageFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// The connection string to use when connecting to SQL Server, or the name of the connection string.
    /// </summary>
    public string NameOrConnectionString { get; set; } = default!;
    
    /// <summary>
    /// Configures the SQL Server storage options.
    /// </summary>
    public Action<SQLiteStorageOptions> ConfigureSqlServerStorageOptions { get; set; } = _ => { };

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Use<HangfireFeature>(hangfireFeature =>
        {
            var storageOptions = new SQLiteStorageOptions
            {
                QueuePollInterval = TimeSpan.FromSeconds(1)
            };
            ConfigureSqlServerStorageOptions(storageOptions);
            
            hangfireFeature.ConfigureHangfire = (_, cfg) =>
            {
                cfg.UseSQLiteStorage(NameOrConnectionString, storageOptions);
            };
            
            hangfireFeature.CreateJobStorage = () => new SQLiteStorage(NameOrConnectionString, storageOptions);
        });
    }
}