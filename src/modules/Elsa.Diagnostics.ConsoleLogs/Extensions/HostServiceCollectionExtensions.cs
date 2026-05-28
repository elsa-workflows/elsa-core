using ConsoleLogStreaming.Core.DependencyInjection;
using ConsoleLogStreaming.Core.Options;
using ConsoleLogStreaming.Persistence.Sqlite.DependencyInjection;
using Elsa.Diagnostics.ConsoleLogs.RealTime;
using Elsa.Diagnostics.ConsoleLogs.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Diagnostics.ConsoleLogs.Extensions;

public static class HostServiceCollectionExtensions
{
    /// <summary>
    /// Registers the host-level console capture pipeline.
    /// </summary>
    public static IServiceCollection AddConsoleLogsHost(this IServiceCollection services, Action<ConsoleLogOptions>? configure = null)
    {
        services.AddConsoleLogContextServices();
        services.AddConsoleLogStreaming(options =>
        {
            ElsaConsoleLogOptions.ConfigureDefaults(options);
            configure?.Invoke(options);
        });
        services.AddConsoleLogStreamingSqlite(options =>
        {
            options.ConnectionString = "Data Source=console-log-streaming-vanilla-sample.db";
            options.MaxAge = TimeSpan.FromHours(12);
            options.MaxRows = 10_000;
        });
        services.AddSignalR();
        services.DecorateConsoleLogProvider();
        services.TryAddSingleton<IElsaConsoleLogHubAuthorizer, ElsaConsoleLogStreamHubAuthorizer>();
        services.TryAddSingleton<ElsaConsoleLogSubscriptionManager>();
        return services;
    }
}
