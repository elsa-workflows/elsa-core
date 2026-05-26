using Elsa.Diagnostics.OpenTelemetry.Options;
using Elsa.Diagnostics.OpenTelemetry.RealTime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.OpenTelemetry.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public const string DefaultHubRoute = "/elsa/hubs/diagnostics/opentelemetry";

    public static void MapOpenTelemetryHub(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<OpenTelemetryDiagnosticsOptions>>().Value;
        endpoints.MapHub<OpenTelemetryHub>(string.IsNullOrWhiteSpace(options.HubRoute) ? DefaultHubRoute : options.HubRoute);
    }
}
