using Elsa.ServerLogs.RealTime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Elsa.ServerLogs.Extensions;

internal static class EndpointRouteBuilderExtensions
{
    public static void MapServerLogStreamingHub(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<ServerLogsHub>("/elsa/hubs/server-logs");
    }
}
