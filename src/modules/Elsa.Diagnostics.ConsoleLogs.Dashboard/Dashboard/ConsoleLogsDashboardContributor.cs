using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.Models;
using Elsa.Dashboard.Abstractions.Contracts;
using Elsa.Dashboard.Abstractions.Models;

namespace Elsa.Diagnostics.ConsoleLogs.Dashboard;

public class ConsoleLogsDashboardContributor(IConsoleLogProvider provider) : IDashboardContributor
{
    public string Id => "diagnostics.console-logs";

    public int Order => 400;

    public async ValueTask<DashboardOverviewContribution?> GetOverviewAsync(DashboardContext context)
    {
        var summary = await GetSummaryAsync(context.Range, context.CancellationToken);
        return new()
        {
            Diagnostics = new()
            {
                ConsoleLogs = summary
            }
        };
    }

    public async ValueTask<IReadOnlyCollection<DashboardFinding>> GetFindingsAsync(DashboardContext context)
    {
        var summary = await GetSummaryAsync(context.Range, context.CancellationToken);
        var findings = new List<DashboardFinding>();

        if (summary.Capability.Status == DashboardCapabilityStatus.Unauthorized.Status)
            findings.Add(Finding("console-log-unauthorized", DashboardFindingSeverity.Warning, "Console log dashboard data is not accessible", "ConsoleLogs", "access", 100));
        else if (summary.Capability.Status == DashboardCapabilityStatus.Unavailable.Status)
            findings.Add(Finding("console-log-unavailable", DashboardFindingSeverity.Warning, "Console log dashboard data is unavailable", "ConsoleLogs", "status", 100));

        if (summary.StaleSourceCount > 0)
            findings.Add(Finding("console-log-stale-sources", DashboardFindingSeverity.Warning, $"{summary.StaleSourceCount} console log sources are stale", "ConsoleLogs", "sources", 100));
        if (summary.DroppedLineCount > 0)
            findings.Add(Finding("console-log-dropped-lines", DashboardFindingSeverity.Warning, "Console log capture dropped lines", "ConsoleLogs", "dropped", 110));

        return findings;
    }

    private async Task<DashboardConsoleLogSummary> GetSummaryAsync(DashboardRange range, CancellationToken cancellationToken)
    {
        try
        {
            var sources = await provider.ListSourcesAsync(cancellationToken);
            var recentStderr = await provider.GetRecentAsync(new()
            {
                Stream = ConsoleStream.Stderr,
                From = range.From,
                To = range.To,
                Limit = 1000
            }, cancellationToken);

            return new()
            {
                Capability = DashboardCapabilityStatus.Available,
                SourceCount = sources.Count,
                StaleSourceCount = sources.Count(x => x.Health is ConsoleLogSourceHealth.Stale or ConsoleLogSourceHealth.Disconnected),
                RecentStderrCount = recentStderr.Items.Count,
                DroppedLineCount = recentStderr.Dropped.Aggregate(0L, (total, x) => checked(total + x.Count))
            };
        }
        catch (UnauthorizedAccessException)
        {
            return new() { Capability = new(DashboardCapabilityStatus.Unauthorized.Status, "No access to console logs") };
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return new() { Capability = new(DashboardCapabilityStatus.Unavailable.Status, "Console log summary is unavailable") };
        }
    }

    private static DashboardFinding Finding(string id, string severity, string message, string? targetKind, string? target, int priority) => new()
    {
        Id = id,
        Severity = severity,
        Message = message,
        TargetKind = targetKind,
        Target = target,
        Priority = priority
    };
}
