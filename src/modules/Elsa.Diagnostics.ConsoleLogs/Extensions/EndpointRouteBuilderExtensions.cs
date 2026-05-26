using ConsoleLogStreaming.SignalR;
using Elsa.Diagnostics.ConsoleLogs.RealTime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.ConsoleLogs.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public const string HubRoute = "/elsa/hubs/diagnostics/console-logs";

    public static void MapConsoleLogsHub(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<ConsoleLogStreamingSignalROptions>>().Value;
        var hub = endpoints.MapHub<ElsaConsoleLogsHub>(options.HubPath);

        if (!string.IsNullOrWhiteSpace(options.AuthorizationPolicy))
            hub.RequireAuthorization(options.AuthorizationPolicy);

        options.ConfigureHubEndpoint?.Invoke(hub);
    }
}
