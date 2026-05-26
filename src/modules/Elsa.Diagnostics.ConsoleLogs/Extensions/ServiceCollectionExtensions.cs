using ConsoleLogStreaming.Contracts;
using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.DependencyInjection;
using ConsoleLogStreaming.Core.Options;
using ConsoleLogStreaming.SignalR;
using ConsoleLogStreaming.SignalR.DependencyInjection;
using Elsa.Diagnostics.ConsoleLogs.Contracts;
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
        services.TryAddSingleton(_ => ConsoleLogContextAccessor.Instance);
        services.AddSingleton<IConsoleLogContextAccessor>(sp => sp.GetRequiredService<ConsoleLogContextAccessor>());
        services.AddSingleton<IConsoleLogMetadataAccessor>(sp => sp.GetRequiredService<ConsoleLogContextAccessor>());
        services.AddConsoleLogStreamingHost(options =>
        {
            ElsaConsoleLogOptions.ConfigureDefaults(options);
            configureOptions?.Invoke(options);
        });
        services.AddConsoleLogStreamingSignalR(options => options.HubPath = EndpointRouteBuilderExtensions.HubRoute);
        services.TryAddSingleton<IConsoleLogStreamingHubAuthorizer, ElsaConsoleLogStreamingHubAuthorizer>();

        return services;
    }
}
