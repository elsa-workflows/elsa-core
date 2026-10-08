using Elsa.Diagnostics.StructuredLogs.RealTime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Elsa.Diagnostics.StructuredLogs.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public const string HubRoute = "/elsa/hubs/diagnostics/structured-logs";

    public static void MapStructuredLogsHub(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<StructuredLogsHub>(HubRoute);
    }
}
