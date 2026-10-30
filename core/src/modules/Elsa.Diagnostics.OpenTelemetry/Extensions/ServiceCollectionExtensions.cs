using Elsa.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Diagnostics.OpenTelemetry.Options;
using Elsa.Diagnostics.OpenTelemetry.Providers.InMemory;
using Elsa.Diagnostics.OpenTelemetry.RealTime;
using Elsa.Diagnostics.OpenTelemetry.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Diagnostics.OpenTelemetry.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenTelemetryDiagnosticsServices(this IServiceCollection services, Action<OpenTelemetryDiagnosticsOptions>? configureOptions = null)
    {
        if (configureOptions != null)
            services.Configure(configureOptions);

        services.AddSignalR();
        services.AddOptions<OpenTelemetryDiagnosticsOptions>();
        services.TryAddSingleton<IOpenTelemetrySourceRegistry, OpenTelemetrySourceRegistry>();
        services.TryAddSingleton<IOpenTelemetryRedactor, OpenTelemetryRedactor>();
        services.TryAddSingleton<InMemoryOpenTelemetryStore>();
        services.TryAddSingleton<IOpenTelemetryStore>(sp => sp.GetRequiredService<InMemoryOpenTelemetryStore>());
        services.TryAddSingleton<InMemoryOpenTelemetryLiveFeed>();
        services.TryAddSingleton<IOpenTelemetryLiveFeed>(sp => sp.GetRequiredService<InMemoryOpenTelemetryLiveFeed>());
        services.TryAddSingleton<IOpenTelemetryIngestor, OpenTelemetryIngestor>();
        services.TryAddSingleton<IOpenTelemetryProvider, DefaultOpenTelemetryProvider>();
        services.TryAddSingleton<ICollectorConfigurationProvider, CollectorConfigurationProvider>();
        services.TryAddSingleton<OpenTelemetrySubscriptionManager>();

        return services;
    }
}
