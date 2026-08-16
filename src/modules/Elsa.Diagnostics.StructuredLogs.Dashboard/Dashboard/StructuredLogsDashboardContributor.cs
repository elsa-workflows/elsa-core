using Elsa.Dashboard.Abstractions.Contracts;
using Elsa.Dashboard.Abstractions.Models;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Models;

namespace Elsa.Diagnostics.StructuredLogs.Dashboard;

public class StructuredLogsDashboardContributor(
    IStructuredLogProvider provider,
    IEnumerable<IStructuredLogStorageDiagnostics> storageDiagnostics) : IDashboardContributor
{
    public string Id => "diagnostics.structured-logs";

    public int Order => 300;

    public async ValueTask<DashboardOverviewContribution?> GetOverviewAsync(DashboardContext context)
    {
        var summary = await GetSummaryAsync(context.Range, context.CancellationToken);
        return new()
        {
            Diagnostics = new()
            {
                StructuredLogs = summary
            }
        };
    }

    public async ValueTask<IReadOnlyCollection<DashboardFinding>> GetFindingsAsync(DashboardContext context)
    {
        var summary = await GetSummaryAsync(context.Range, context.CancellationToken);
        var findings = new List<DashboardFinding>();

        if (summary.Capability.Status == DashboardCapabilityStatus.Unauthorized.Status)
            findings.Add(Finding("structured-log-unauthorized", DashboardFindingSeverity.Warning, "Structured log dashboard data is not accessible", "StructuredLogs", "access", 70));
        else if (summary.Capability.Status == DashboardCapabilityStatus.Unavailable.Status)
            findings.Add(Finding("structured-log-unavailable", DashboardFindingSeverity.Warning, "Structured log dashboard data is unavailable", "StructuredLogs", "status", 70));

        if (summary.StaleSourceCount > 0)
            findings.Add(Finding("structured-log-stale-sources", DashboardFindingSeverity.Warning, $"{summary.StaleSourceCount} structured log sources are stale", "StructuredLogs", "sources", 70));
        if (summary.DroppedWriteCount > 0)
            findings.Add(Finding("structured-log-dropped-writes", DashboardFindingSeverity.Error, "Structured log storage dropped writes", "StructuredLogs", "storage", 80));
        if (summary.RecentErrorOrCriticalCount > 0)
            findings.Add(Finding("structured-log-errors", DashboardFindingSeverity.Error, $"{summary.RecentErrorOrCriticalCount} error or critical structured logs were recorded", "StructuredLogs", "errors", 90));

        return findings;
    }

    private async Task<DashboardStructuredLogSummary> GetSummaryAsync(DashboardRange range, CancellationToken cancellationToken)
    {
        try
        {
            var sources = await provider.ListSourcesAsync(cancellationToken);
            var recentErrors = await provider.GetRecentAsync(new()
            {
                Levels = [StructuredLogLevel.Error, StructuredLogLevel.Critical],
                From = range.From,
                To = range.To,
                Take = 1000
            }, cancellationToken);

            return new()
            {
                Capability = DashboardCapabilityStatus.Available,
                SourceCount = sources.Count,
                StaleSourceCount = sources.Count(x => x.Status == StructuredLogSourceStatus.Stale || x.Status == StructuredLogSourceStatus.Disconnected),
                RecentErrorOrCriticalCount = recentErrors.Items.Count,
                DroppedWriteCount = storageDiagnostics.Aggregate(0L, (total, x) => checked(total + x.DroppedWriteCount)),
                DroppedEventCount = recentErrors.DroppedEvents
            };
        }
        catch (UnauthorizedAccessException)
        {
            return new() { Capability = new(DashboardCapabilityStatus.Unauthorized.Status, "No access to structured logs") };
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return new() { Capability = new(DashboardCapabilityStatus.Unavailable.Status, "Structured log summary is unavailable") };
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
