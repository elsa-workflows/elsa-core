using CShells.AspNetCore.Features;
using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.Diagnostics.StructuredLogs.Extensions;
using Elsa.Diagnostics.StructuredLogs.Options;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Diagnostics.StructuredLogs.ShellFeatures;

/// <summary>
/// Provides live structured log streaming over REST and SignalR.
/// </summary>
[ShellFeature(
    DisplayName = "Structured Logs",
    Description = "Provides live structured log streaming over REST and SignalR",
    DependsOn = ["ElsaFastEndpoints"])]
[UsedImplicitly]
public class StructuredLogsFeature : IFastEndpointsShellFeature, IWebShellFeature
{
    private static readonly StructuredLogsOptions DefaultOptions = new();

    public int RecentLogCapacity { get; set; } = DefaultOptions.RecentLogCapacity;
    public int SubscriberChannelCapacity { get; set; } = DefaultOptions.SubscriberChannelCapacity;
    public int MaxRecentLogQuerySize { get; set; } = DefaultOptions.MaxRecentLogQuerySize;
    public TimeSpan SourceHeartbeatTimeout { get; set; } = DefaultOptions.SourceHeartbeatTimeout;
    public bool IncludeStructuredLogsInternalLogs { get; set; } = DefaultOptions.IncludeStructuredLogsInternalLogs;
    public ICollection<string> SensitiveNames { get; set; } = [..DefaultOptions.SensitiveNames];
    public ICollection<string> SensitiveTextPatterns { get; set; } = [..DefaultOptions.SensitiveTextPatterns];

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddStructuredLogsServices(ConfigureOptions);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints, IHostEnvironment? environment)
    {
        endpoints.MapStructuredLogsHub();
    }

    private void ConfigureOptions(StructuredLogsOptions options)
    {
        options.RecentLogCapacity = RecentLogCapacity;
        options.SubscriberChannelCapacity = SubscriberChannelCapacity;
        options.MaxRecentLogQuerySize = MaxRecentLogQuerySize;
        options.SourceHeartbeatTimeout = SourceHeartbeatTimeout;
        options.IncludeStructuredLogsInternalLogs = IncludeStructuredLogsInternalLogs;
        options.SensitiveNames = [..SensitiveNames];
        options.SensitiveTextPatterns = [..SensitiveTextPatterns];
    }
}
