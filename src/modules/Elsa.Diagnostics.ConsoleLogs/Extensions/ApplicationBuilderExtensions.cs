using Elsa.Diagnostics.ConsoleLogs.Extensions;
using Microsoft.AspNetCore.Routing;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ConsoleLogsApplicationBuilderExtensions
{
    public static IEndpointRouteBuilder UseConsoleLogs(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapConsoleLogsHub();
        return endpoints;
    }
}
