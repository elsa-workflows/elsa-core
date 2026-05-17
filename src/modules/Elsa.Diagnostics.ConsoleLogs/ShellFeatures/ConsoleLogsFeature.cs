using CShells.AspNetCore.Features;
using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.Diagnostics.ConsoleLogs.Extensions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Diagnostics.ConsoleLogs.ShellFeatures;

/// <summary>
/// Provides live raw console log streaming over REST and SignalR.
/// </summary>
[ShellFeature(
    DisplayName = "Console Logs",
    Description = "Provides live raw console log streaming over REST and SignalR",
    DependsOn = ["ElsaFastEndpoints"])]
[UsedImplicitly]
public class ConsoleLogsFeature : IFastEndpointsShellFeature, IWebShellFeature
{
    private static readonly ConsoleLogsOptions DefaultOptions = new();

    public int RecentLogCapacity { get; set; } = DefaultOptions.RecentLogCapacity;
    public int SubscriberChannelCapacity { get; set; } = DefaultOptions.SubscriberChannelCapacity;
    public int MaxRecentQuerySize { get; set; } = DefaultOptions.MaxRecentQuerySize;
    public int MaxLineLength { get; set; } = DefaultOptions.MaxLineLength;
    public TimeSpan IdleFlushTimeout { get; set; } = DefaultOptions.IdleFlushTimeout;
    public bool StripAnsiEscapeSequences { get; set; } = DefaultOptions.StripAnsiEscapeSequences;
    public TimeSpan SourceHeartbeatTimeout { get; set; } = DefaultOptions.SourceHeartbeatTimeout;
    public bool IncludeConsoleLogsInternalLogs { get; set; } = DefaultOptions.IncludeConsoleLogsInternalLogs;
    public ICollection<string> SensitiveNames { get; set; } = [..DefaultOptions.SensitiveNames];
    public ICollection<string> SensitiveTextPatterns { get; set; } = [..DefaultOptions.SensitiveTextPatterns];

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddConsoleLogsServices(ConfigureOptions);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints, IHostEnvironment? environment)
    {
        endpoints.MapConsoleLogsHub();
    }

    private void ConfigureOptions(ConsoleLogsOptions options)
    {
        options.RecentLogCapacity = RecentLogCapacity;
        options.SubscriberChannelCapacity = SubscriberChannelCapacity;
        options.MaxRecentQuerySize = MaxRecentQuerySize;
        options.MaxLineLength = MaxLineLength;
        options.IdleFlushTimeout = IdleFlushTimeout;
        options.StripAnsiEscapeSequences = StripAnsiEscapeSequences;
        options.SourceHeartbeatTimeout = SourceHeartbeatTimeout;
        options.IncludeConsoleLogsInternalLogs = IncludeConsoleLogsInternalLogs;
        options.SensitiveNames = [..SensitiveNames];
        options.SensitiveTextPatterns = [..SensitiveTextPatterns];
    }
}
