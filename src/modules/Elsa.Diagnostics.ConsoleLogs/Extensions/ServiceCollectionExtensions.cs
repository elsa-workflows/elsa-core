using Elsa.Diagnostics.ConsoleLogs.RealTime;
using Elsa.Diagnostics.ConsoleLogs.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.ConsoleLogs.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers shell-level console-log consumers (SignalR, subscription manager) and exposes the
    /// process-wide capture pipeline owned by <see cref="ConsoleLogsHost"/> through DI. Configuration
    /// supplied here applies only if the host has not yet been initialised (first-wins). Prefer calling
    /// <c>AddConsoleLogsHost</c> from <c>Program.cs</c> for deterministic host-level configuration.
    /// </summary>
    public static IServiceCollection AddConsoleLogsServices(this IServiceCollection services, Action<ConsoleLogsOptions>? configureOptions = null)
    {
        ConsoleStreamHook.Install();

        if (configureOptions != null)
            ConsoleLogsHost.Configure(configureOptions);

        services.AddSignalR();

        // Every shell resolves the same process-wide singletons — console output is a shared OS resource.
        services.TryAddSingleton<IOptions<ConsoleLogsOptions>>(_ => ConsoleLogsHost.Options);
        services.TryAddSingleton<IConsoleLogSourceRegistry>(_ => ConsoleLogsHost.SourceRegistry);
        services.TryAddSingleton<IConsoleLogRedactor>(_ => ConsoleLogsHost.Redactor);
        services.TryAddSingleton(_ => ConsoleLogsHost.Formatter);
        services.TryAddSingleton<IConsoleLogProvider>(_ => ConsoleLogsHost.Provider);

        // The subscription manager wraps a per-shell SignalR hub context, so it must be per-shell.
        services.TryAddSingleton<ConsoleLogSubscriptionManager>();

        // Ensure the host is initialised even if AddConsoleLogsHost was not called from Program.cs.
        ConsoleLogsHost.EnsureInitialized();

        return services;
    }
}
