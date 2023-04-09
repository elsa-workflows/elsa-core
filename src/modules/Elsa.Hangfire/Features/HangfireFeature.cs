using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.SqlServer;
using Newtonsoft.Json;

namespace Elsa.Hangfire.Features;

/// <summary>
/// Sets up Hangfire.
/// </summary>
public class HangfireFeature : FeatureBase
{
    /// <inheritdoc />
    public HangfireFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// Whether to use SQL Server storage.
    /// </summary>
    public bool UseSqlServerStorage { get; set; }
    
    /// <summary>
    /// The SQL Server storage options.
    /// </summary>
    public SqlServerStorageOptions? SqlServerStorageOptions { get; set; }
    
    /// <summary>
    /// The SQL Server connection string.
    /// </summary>
    public string? SqlServerConnectionString { get; set; }
    
    /// <summary>
    /// The Hangfire background server options.
    /// </summary>
    public Action<BackgroundJobServerOptions>? ConfigureBackgroundServerOptions { get; set; }

    /// <inheritdoc />
    public override void Configure()
    {
        Services.AddHangfire(configuration =>
        {
            configuration.UseSimpleAssemblyNameTypeSerializer();
            configuration.UseRecommendedSerializerSettings(json => json.TypeNameHandling = TypeNameHandling.Objects);

            if (UseSqlServerStorage)
            {
                var storageOptions = SqlServerStorageOptions ?? new SqlServerStorageOptions();
                configuration.UseSqlServerStorage(SqlServerConnectionString, storageOptions);
            }
            else
            {
                configuration.UseMemoryStorage();
            }
        });

        if (UseSqlServerStorage)
            Services.AddHangfireServer((_, options) => ConfigureBackgroundServerOptions?.Invoke(options), new SqlServerStorage(SqlServerConnectionString));
        else
            Services.AddHangfireServer(options => { ConfigureBackgroundServerOptions?.Invoke(options); });
    }
}