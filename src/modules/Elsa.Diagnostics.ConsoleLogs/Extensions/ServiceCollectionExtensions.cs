using ConsoleLogStream.Core;
using ConsoleLogStream.Core.DependencyInjection;
using ConsoleLogStream.Core.Options;
using ConsoleLogStream.Core.Providers;
using Elsa.Diagnostics.ConsoleLogs.RealTime;
using Elsa.Diagnostics.ConsoleLogs.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Diagnostics.ConsoleLogs.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers shell-level console-log consumers and exposes the process-wide capture pipeline through DI.
    /// </summary>
    public static IServiceCollection AddConsoleLogsServices(this IServiceCollection services, Action<ConsoleLogOptions>? configureOptions = null)
    {
        services.AddConsoleLogContextServices();
        services.AddConsoleLogStream(options =>
        {
            ElsaConsoleLogOptions.ConfigureDefaults(options);
            configureOptions?.Invoke(options);
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
