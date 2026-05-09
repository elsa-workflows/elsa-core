using CShells.AspNetCore.Features;
using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.ServerLogs.Extensions;
using Elsa.ServerLogs.Options;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.ServerLogs.ShellFeatures;

/// <summary>
/// Provides live server log streaming over REST and SignalR.
/// </summary>
[ShellFeature(
    DisplayName = "Server Logs",
    Description = "Provides live server log streaming over REST and SignalR",
    DependsOn = ["ElsaFastEndpoints"])]
[UsedImplicitly]
public class ServerLogStreamingFeature : IFastEndpointsShellFeature, IWebShellFeature
{
    public Action<ServerLogStreamingOptions>? ConfigureOptions { get; set; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddServerLogStreamingServices(ConfigureOptions);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints, IHostEnvironment? environment)
    {
        endpoints.MapServerLogStreamingHub();
    }
}
