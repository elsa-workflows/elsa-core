using Elsa.Dashboard.Abstractions.Extensions;
using Elsa.Diagnostics.StructuredLogs.Dashboard;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Logging;
using Elsa.Diagnostics.StructuredLogs.Options;
using Elsa.Diagnostics.StructuredLogs.Providers.InMemory;
using Elsa.Diagnostics.StructuredLogs.RealTime;
using Elsa.Diagnostics.StructuredLogs.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Elsa.Diagnostics.StructuredLogs.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStructuredLogsServices(this IServiceCollection services, Action<StructuredLogsOptions>? configureOptions = null)
    {
        if (configureOptions != null)
            services.Configure(configureOptions);

        services.AddSignalR();
        services.AddOptions<StructuredLogsOptions>();
        services.TryAddSingleton<IStructuredLogSourceRegistry, StructuredLogSourceRegistry>();
        services.TryAddSingleton<IStructuredLogRedactor, StructuredLogRedactor>();
        services.TryAddSingleton<InMemoryStructuredLogStore>();
        services.TryAddSingleton<IStructuredLogStore>(sp => sp.GetRequiredService<InMemoryStructuredLogStore>());
        services.TryAddSingleton<InMemoryStructuredLogLiveFeed>();
        services.TryAddSingleton<IStructuredLogLiveFeed>(sp => sp.GetRequiredService<InMemoryStructuredLogLiveFeed>());
        services.TryAddSingleton<IStructuredLogProvider, DefaultStructuredLogProvider>();
        services.TryAddSingleton<StructuredLogSubscriptionManager>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, StructuredLogLoggerProvider>());
        services.AddDashboardContributor<StructuredLogsDashboardContributor>();

        return services;
    }
}
