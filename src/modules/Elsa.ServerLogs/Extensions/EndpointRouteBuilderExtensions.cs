using Elsa.ServerLogs.Permissions;
using Elsa.ServerLogs.RealTime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Elsa.ServerLogs.Extensions;

internal static class EndpointRouteBuilderExtensions
{
    public static void MapServerLogStreamingHub(this IEndpointRouteBuilder endpoints)
    {
        var hub = endpoints.MapHub<ServerLogsHub>("/elsa/hubs/server-logs");

        if (EndpointSecurityOptions.SecurityIsEnabled)
            hub.RequireAuthorization(policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context => ServerLogPermissions.CanRead(context.User)));
    }
}
