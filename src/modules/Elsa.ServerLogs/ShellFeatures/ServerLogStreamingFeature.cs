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
    private static readonly ServerLogStreamingOptions DefaultOptions = new();

    public int RecentLogCapacity { get; set; } = DefaultOptions.RecentLogCapacity;
    public int SubscriberChannelCapacity { get; set; } = DefaultOptions.SubscriberChannelCapacity;
    public int MaxRecentLogQuerySize { get; set; } = DefaultOptions.MaxRecentLogQuerySize;
    public TimeSpan SourceHeartbeatTimeout { get; set; } = DefaultOptions.SourceHeartbeatTimeout;
    public bool IncludeServerLogsInternalLogs { get; set; } = DefaultOptions.IncludeServerLogsInternalLogs;
    public ICollection<string> SensitiveNames { get; set; } = [..DefaultOptions.SensitiveNames];
    public ICollection<string> SensitiveTextPatterns { get; set; } = [..DefaultOptions.SensitiveTextPatterns];

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddServerLogStreamingServices(ConfigureOptions);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints, IHostEnvironment? environment)
    {
        endpoints.MapServerLogStreamingHub();
    }

    private void ConfigureOptions(ServerLogStreamingOptions options)
    {
        options.RecentLogCapacity = RecentLogCapacity;
        options.SubscriberChannelCapacity = SubscriberChannelCapacity;
        options.MaxRecentLogQuerySize = MaxRecentLogQuerySize;
        options.SourceHeartbeatTimeout = SourceHeartbeatTimeout;
        options.IncludeServerLogsInternalLogs = IncludeServerLogsInternalLogs;
        options.SensitiveNames = [..SensitiveNames];
        options.SensitiveTextPatterns = [..SensitiveTextPatterns];
    }
}
