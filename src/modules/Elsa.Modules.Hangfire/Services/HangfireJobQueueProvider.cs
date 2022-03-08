using Elsa.Jobs.Contracts;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Modules.Hangfire.Services;

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

    public void ConfigureServices(IServiceCollection services)
    {
        if (RegisterHangfire)
        {
            var storageOptions = SqlServerStorageOptions ?? new SqlServerStorageOptions();
            
            services
                .AddHangfire(configuration => configuration
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseSqlServerStorage(SqlServerConnectionString, storageOptions));
        }

        services.AddSingleton<IJobQueue, HangfireJobQueue>();
    }
}