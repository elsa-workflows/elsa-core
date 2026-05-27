using CShells.AspNetCore.Features;
using CShells.FastEndpoints.Features;
using CShells.Features;
using ConsoleLogStreaming.Core.Options;
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
    DependsOn = ["ElsaFastEndpoints", "Workflows"])]
[UsedImplicitly]
public class ConsoleLogsFeature : IFastEndpointsShellFeature, IWebShellFeature
{
    private static readonly ConsoleLogOptions DefaultOptions = new();

    public int RecentLogCapacity { get; set; } = DefaultOptions.RecentCapacity;
    public int SubscriberChannelCapacity { get; set; } = DefaultOptions.SubscriberCapacity;
    public int MaxRecentQuerySize { get; set; } = DefaultOptions.MaxRecentQuerySize;
    public int MaxLineLength { get; set; } = DefaultOptions.MaxLineLength;
    public TimeSpan IdleFlushTimeout { get; set; } = DefaultOptions.IdleFlushTimeout;
    public bool StripAnsiEscapeSequences { get; set; } = !DefaultOptions.PreserveAnsi;
    public string RedactionReplacement { get; set; } = "[Redacted]";
    public ICollection<string> SensitiveTextPatterns { get; set; } = [..DefaultOptions.RedactionRules.Select(x => x.Pattern)];

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddConsoleLogsServices(ConfigureOptions);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints, IHostEnvironment? environment)
    {
        endpoints.MapConsoleLogsHub();
    }

    private void ConfigureOptions(ConsoleLogOptions options)
    {
        options.RecentCapacity = RecentLogCapacity;
        options.SubscriberCapacity = SubscriberChannelCapacity;
        options.MaxRecentQuerySize = MaxRecentQuerySize;
        options.MaxLineLength = MaxLineLength;
        options.IdleFlushTimeout = IdleFlushTimeout;
        options.PreserveAnsi = !StripAnsiEscapeSequences;
        options.RedactionRules.Clear();
        foreach (var pattern in SensitiveTextPatterns)
        {
            options.RedactionRules.Add(new()
            {
                Name = "Sensitive text",
                Pattern = pattern,
                Replacement = RedactionReplacement
            });
        }
    }
}
