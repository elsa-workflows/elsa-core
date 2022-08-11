using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.SqlServer;
using Newtonsoft.Json;

namespace Elsa.Hangfire.Features;

public class HangfireFeature : FeatureBase
{
    public HangfireFeature(IModule module) : base(module)
    {
    }

    public bool UseSqlServerStorage { get; set; }
    public SqlServerStorageOptions? SqlServerStorageOptions { get; set; }
    public string? SqlServerConnectionString { get; set; }
    public Action<BackgroundJobServerOptions>? ConfigureBackgroundServerOptions { get; set; }

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