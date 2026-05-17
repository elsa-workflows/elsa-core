using Elsa.Diagnostics.ConsoleLogs.Providers.InMemory;
using Elsa.Diagnostics.ConsoleLogs.RealTime;
using Elsa.Diagnostics.ConsoleLogs.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Diagnostics.ConsoleLogs.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConsoleLogsServices(this IServiceCollection services, Action<ConsoleLogsOptions>? configureOptions = null)
    {
        if (configureOptions != null)
            services.Configure(configureOptions);

        services.AddSignalR();
        services.AddOptions<ConsoleLogsOptions>();
        services.TryAddSingleton<IConsoleLogSourceRegistry, ConsoleLogSourceRegistry>();
        services.TryAddSingleton<IConsoleLogRedactor, ConsoleLogRedactor>();
        services.TryAddSingleton<ConsoleLineFormatter>();
        services.TryAddSingleton<IConsoleLogProvider, InMemoryConsoleLogProvider>();
        services.TryAddSingleton<ConsoleLogSubscriptionManager>();
        services.TryAddSingleton<IConsoleLogCapture, ConsoleCaptureTee>();
        services.AddHostedService<ConsoleLogCaptureHostedService>();
        services.AddHostedService<ConsoleLogSourceHealthService>();

        return services;
    }
}
