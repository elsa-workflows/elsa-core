using Elsa.ServerLogs.Contracts;
using Elsa.ServerLogs.Logging;
using Elsa.ServerLogs.Options;
using Elsa.ServerLogs.Providers.InMemory;
using Elsa.ServerLogs.RealTime;
using Elsa.ServerLogs.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Elsa.ServerLogs.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServerLogStreamingServices(this IServiceCollection services, Action<ServerLogStreamingOptions>? configureOptions = null)
    {
        if (configureOptions != null)
            services.Configure(configureOptions);

        services.AddSignalR();
        services.AddOptions<ServerLogStreamingOptions>();
        services.TryAddSingleton<IServerLogSourceRegistry, ServerLogSourceRegistry>();
        services.TryAddSingleton<IServerLogRedactor, ServerLogRedactor>();
        services.TryAddSingleton<IServerLogProvider, InMemoryServerLogProvider>();
        services.TryAddSingleton<ServerLogSubscriptionManager>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ServerLogLoggerProvider>());

        return services;
    }
}
