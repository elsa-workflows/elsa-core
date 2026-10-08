using Elsa.Diagnostics.StructuredLogs.Extensions;
using Microsoft.AspNetCore.Builder;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class StructuredLogsApplicationBuilderExtensions
{
    public static IApplicationBuilder UseStructuredLogs(this IApplicationBuilder app)
    {
        return app.UseEndpoints(endpoints =>
        {
            endpoints.MapStructuredLogsHub();
        });
    }
}
