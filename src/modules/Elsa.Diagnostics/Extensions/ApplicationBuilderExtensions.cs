using System.Security.Claims;
using Elsa.Diagnostics.Permissions;
using Elsa.Diagnostics.RealTime;
using Microsoft.AspNetCore.Builder;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ServerLogStreamingApplicationBuilderExtensions
{
    public static IApplicationBuilder UseServerLogStreaming(this IApplicationBuilder app)
    {
        return app.UseEndpoints(endpoints =>
        {
            var hub = endpoints.MapHub<ServerLogsHub>("/elsa/hubs/server-logs");

            if (EndpointSecurityOptions.SecurityIsEnabled)
                hub.RequireAuthorization(policy => policy
                    .RequireAuthenticatedUser()
                    .RequireAssertion(context => HasServerLogPermission(context.User)));
        });
    }

    private static bool HasServerLogPermission(ClaimsPrincipal user)
    {
        return user.HasClaim("permissions", PermissionNames.All) || user.HasClaim("permissions", ServerLogPermissions.Read);
    }
}
