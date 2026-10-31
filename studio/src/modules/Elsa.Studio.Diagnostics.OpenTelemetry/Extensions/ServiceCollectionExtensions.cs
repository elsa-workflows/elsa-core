using Elsa.Studio.Contracts;
using Elsa.Studio.Diagnostics.OpenTelemetry.Client;
using Elsa.Studio.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Studio.Diagnostics.OpenTelemetry.Menu;
using Elsa.Studio.Diagnostics.OpenTelemetry.Services;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Extensions;

/// <summary>
/// Contains extension methods for the OpenTelemetry diagnostics module.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the OpenTelemetry diagnostics module.
    /// </summary>
    public static IServiceCollection AddOpenTelemetryDiagnosticsModule(this IServiceCollection services, BackendApiConfig backendApiConfig)
    {
        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMenuProvider, OpenTelemetryMenu>()
            .AddScoped<IOpenTelemetryService, RemoteOpenTelemetryService>()
            .AddTransient<IOpenTelemetryObserver, SignalROpenTelemetryObserver>()
            .AddRemoteApi<IOpenTelemetryApi>(backendApiConfig);
    }
}
