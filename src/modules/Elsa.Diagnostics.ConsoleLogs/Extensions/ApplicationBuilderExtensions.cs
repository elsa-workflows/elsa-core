using Elsa.Diagnostics.ConsoleLogs.Extensions;
using Microsoft.AspNetCore.Builder;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ConsoleLogsApplicationBuilderExtensions
{
    public static IApplicationBuilder UseConsoleLogs(this IApplicationBuilder app)
    {
        return app.UseEndpoints(endpoints =>
        {
            endpoints.MapConsoleLogsHub();
        });
    }
}
