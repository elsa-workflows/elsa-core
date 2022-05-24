using Elsa.Jobs.Services;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Elsa.Hangfire.Implementations;

public class HangfireJobQueueProvider : IJobQueueProvider
{
    public HangfireJobQueueProvider(bool registerHangfire = true)
    {
        RegisterHangfire = registerHangfire;
    }

    public HangfireJobQueueProvider(string sqlServerConnectionString)
    {
        SqlServerConnectionString = sqlServerConnectionString;
        UseSqlServerStorage = true;
        RegisterHangfire = true;
    }

    public bool RegisterHangfire { get; set; }
    public bool UseSqlServerStorage { get; set; }
    public string? SqlServerConnectionString { get; set; }
    public SqlServerStorageOptions? SqlServerStorageOptions { get; set; }
    public Action<BackgroundJobServerOptions>? ConfigureBackgroundServerOptions { get; set; }

    public void ConfigureServices(IServiceCollection services)
    {
        if (RegisterHangfire)
        {
            services.AddHangfire(configuration =>
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
                services.AddHangfireServer((_, options) => ConfigureBackgroundServerOptions?.Invoke(options), new SqlServerStorage(SqlServerConnectionString));
            else
                services.AddHangfireServer(options =>
                {
                    ConfigureBackgroundServerOptions?.Invoke(options);
                });
        }

        services.AddSingleton<IJobQueue, HangfireJobQueue>();
    }
}