using ConsoleLogStreaming.Core.DependencyInjection;
using ConsoleLogStreaming.Core.Options;
using CShells.Lifecycle;
using Elsa.Dashboard.Abstractions.Extensions;
using Elsa.Diagnostics.ConsoleLogs.Dashboard;
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
        services.AddConsoleLogStreaming(options =>
        {
            ElsaConsoleLogOptions.ConfigureDefaults(options);
            configureOptions?.Invoke(options);
        });
        services.AddSignalR();
        services.DecorateConsoleLogProvider();
        services.TryAddSingleton<IElsaConsoleLogHubAuthorizer, ElsaConsoleLogStreamHubAuthorizer>();
        services.TryAddSingleton<ElsaConsoleLogSubscriptionManager>();
        services.TryAddScoped<ConsoleLogCaptureShellLease>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IShellInitializer, ConsoleLogCaptureShellInitializer>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IDrainHandler, ConsoleLogCaptureShellDrainHandler>());
        services.AddDashboardContributor<ConsoleLogsDashboardContributor>();

        return services;
    }
}
