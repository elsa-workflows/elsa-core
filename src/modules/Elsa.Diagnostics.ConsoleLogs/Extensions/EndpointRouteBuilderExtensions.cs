using Elsa.Diagnostics.ConsoleLogs.RealTime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Elsa.Diagnostics.ConsoleLogs.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public const string HubRoute = "/elsa/hubs/diagnostics/console-logs";

    public static void MapConsoleLogsHub(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<ElsaConsoleLogsHub>(HubRoute);
    }
}
