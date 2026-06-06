using CShells.AspNetCore.Features;
using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.Diagnostics.StructuredLogs.Extensions;
using Elsa.Diagnostics.StructuredLogs.Options;
using Elsa.Platform.PackageManifest.Generator.Hints;
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
    DependsOn = [typeof(global::Elsa.ShellFeatures.ElsaFastEndpointsFeature)])]
[UsedImplicitly]
public class StructuredLogsFeature : IFastEndpointsShellFeature, IWebShellFeature
{
    private const string SensitiveNamesDefaultValue = "authorization, token, password, secret, api-key, apikey, cookie, connection-string, connectionstring";
    private const string SensitiveTextPatternsDefaultValue = "(?i)bearer\\s+[A-Za-z0-9._~+/=-]+; (?i)(password|secret|token|api[-_]?key)\\s*[=:]\\s*[^\\s,;]+; (?i)(AccountKey|SharedAccessKey)=([^;\\s]+)";
    private static readonly StructuredLogsOptions DefaultOptions = new();

    [ManifestSetting(
        DisplayName = "Recent Log Capacity",
        Description = "Maximum number of recent log events retained in memory.",
        Category = "Diagnostics",
        DefaultValue = "5000",
        RestartRequired = true)]
    public int RecentLogCapacity { get; set; } = DefaultOptions.RecentLogCapacity;

    [ManifestSetting(
        DisplayName = "Subscriber Channel Capacity",
        Description = "Maximum number of queued log events per streaming subscriber.",
        Category = "Diagnostics",
        DefaultValue = "1000",
        RestartRequired = true)]
    public int SubscriberChannelCapacity { get; set; } = DefaultOptions.SubscriberChannelCapacity;

    [ManifestSetting(
        DisplayName = "Maximum Recent Log Query Size",
        Description = "Maximum number of recent log events returned by a query.",
        Category = "Diagnostics",
        DefaultValue = "1000",
        RestartRequired = true)]
    public int MaxRecentLogQuerySize { get; set; } = DefaultOptions.MaxRecentLogQuerySize;

    [ManifestSetting(
        DisplayName = "Source Heartbeat Timeout",
        Description = "Time after which a structured log source is considered inactive.",
        Category = "Diagnostics",
        DefaultValue = "00:00:30",
        RestartRequired = true)]
    public TimeSpan SourceHeartbeatTimeout { get; set; } = DefaultOptions.SourceHeartbeatTimeout;

    [ManifestSetting(
        DisplayName = "Include Internal Structured Logs",
        Description = "Include log events produced by the structured logs subsystem itself.",
        Category = "Diagnostics",
        DefaultValue = "false",
        Advanced = true,
        RestartRequired = true)]
    public bool IncludeStructuredLogsInternalLogs { get; set; } = DefaultOptions.IncludeStructuredLogsInternalLogs;

    [ManifestSetting(
        DisplayName = "Sensitive Names",
        Description = "Property names whose values should be redacted from structured log events.",
        Category = "Redaction",
        DefaultValue = SensitiveNamesDefaultValue,
        RestartRequired = true)]
    public ICollection<string> SensitiveNames { get; set; } = [..DefaultOptions.SensitiveNames];

    [ManifestSetting(
        DisplayName = "Sensitive Text Patterns",
        Description = "Text patterns whose values should be redacted from structured log events.",
        Category = "Redaction",
        DefaultValue = SensitiveTextPatternsDefaultValue,
        Advanced = true,
        RestartRequired = true)]
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
