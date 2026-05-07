using Elsa.Diagnostics.RealTime;
using Microsoft.AspNetCore.Builder;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ServerLogStreamingApplicationBuilderExtensions
{
    public static IApplicationBuilder UseServerLogStreaming(this IApplicationBuilder app)
    {
        return app.UseEndpoints(endpoints => endpoints.MapHub<ServerLogsHub>("/elsa/hubs/server-logs"));
    }
}
