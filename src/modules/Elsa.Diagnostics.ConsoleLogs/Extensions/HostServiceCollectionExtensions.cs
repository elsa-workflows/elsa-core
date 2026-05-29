using ConsoleLogStreaming.Core.DependencyInjection;
using ConsoleLogStreaming.Core.Options;
using Elsa.Diagnostics.ConsoleLogs.RealTime;
using Elsa.Diagnostics.ConsoleLogs.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

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
        services.AddSignalR();
        services.DecorateConsoleLogProvider();
        services.TryAddSingleton<IElsaConsoleLogHubAuthorizer, ElsaConsoleLogStreamHubAuthorizer>();
        services.TryAddSingleton<ElsaConsoleLogSubscriptionManager>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ConsoleLogCaptureHostedService>());
        return services;
    }
}
