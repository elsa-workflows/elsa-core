using ConsoleLogStreaming.Contracts;
using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.DependencyInjection;
using ConsoleLogStreaming.Core.Options;
using ConsoleLogStreaming.SignalR;
using ConsoleLogStreaming.SignalR.DependencyInjection;
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
        services.AddConsoleLogStreamingHost(options =>
        {
            ElsaConsoleLogOptions.ConfigureDefaults(options);
            configureOptions?.Invoke(options);
        });
        services.AddConsoleLogStreamingSignalR(options => options.HubPath = EndpointRouteBuilderExtensions.HubRoute);
        services.TryAddSingleton<IConsoleLogStreamingHubAuthorizer, ElsaConsoleLogStreamingHubAuthorizer>();
        services.TryAddSingleton<ElsaConsoleLogSubscriptionManager>();

        return services;
    }
}
