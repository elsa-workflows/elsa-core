using ConsoleLogStream.Core;
using ConsoleLogStream.Core.DependencyInjection;
using ConsoleLogStream.Core.Options;
using ConsoleLogStream.Core.Providers;
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
        services.AddConsoleLogStream(options =>
        {
            ElsaConsoleLogOptions.ConfigureDefaults(options);
            configure?.Invoke(options);
        });
        services.AddSignalR();
        services.DecorateConsoleLogProvider();
        services.TryAddSingleton<IElsaConsoleLogHubAuthorizer, ElsaConsoleLogStreamHubAuthorizer>();
        services.TryAddSingleton<ElsaConsoleLogSubscriptionManager>();
        return services;
    }

    private static void DecorateConsoleLogProvider(this IServiceCollection services)
    {
        services.RemoveAll<IConsoleLogProvider>();
        services.TryAddSingleton<InMemoryConsoleLogProvider>();
        services.TryAddSingleton<IConsoleLogProvider>(sp => new ElsaConsoleLogProvider(
            sp.GetRequiredService<InMemoryConsoleLogProvider>(),
            sp.GetRequiredService<IConsoleLogContextAccessor>()));
    }
}
