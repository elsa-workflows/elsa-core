using Elsa.ServerLogs.Extensions;
using Microsoft.AspNetCore.Builder;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ServerLogStreamingApplicationBuilderExtensions
{
    public static IApplicationBuilder UseServerLogStreaming(this IApplicationBuilder app)
    {
        return app.UseEndpoints(endpoints =>
        {
            endpoints.MapServerLogStreamingHub();
        });
    }
}
