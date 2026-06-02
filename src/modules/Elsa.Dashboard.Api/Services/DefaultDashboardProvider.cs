using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.Models;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Dashboard.Api.Contracts;
using Elsa.Dashboard.Api.Models;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Enums;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Dashboard.Api.Services;

public class DefaultDashboardProvider(
    IWorkflowInstanceStore workflowInstanceStore,
    IWorkflowRuntimeAdminService runtimeAdminService,
    DashboardRangeResolver rangeResolver,
    IServiceProvider serviceProvider,
    IHostEnvironment environment) : IDashboardProvider
{
    public async Task<DashboardOverview> GetOverviewAsync(DashboardQuery query, CancellationToken cancellationToken = default)
    {
        var range = rangeResolver.Resolve(query.Range);
        var runtime = GetRuntimeStatus();
        var workflowMetrics = await GetWorkflowMetricsAsync(range, query.IncludeSystem, cancellationToken);
        var diagnostics = await GetDiagnosticsSummaryAsync(range, cancellationToken);

        return new()
        {
            BackendName = environment.ApplicationName,
            EnvironmentName = environment.EnvironmentName,
            Runtime = runtime,
            WorkflowInstances = workflowMetrics,
            Diagnostics = diagnostics,
            AppliedRange = range.Key,
            From = range.From,
            To = range.To
        };
    }

    public async Task<DashboardTrendResponse> GetWorkflowTrendsAsync(DashboardTrendRequest request, CancellationToken cancellationToken = default)
    {
        var range = rangeResolver.Resolve(request.Range);
        var granularity = rangeResolver.ResolveGranularity(request.Granularity, range.Key);
        var bucketSize = rangeResolver.GetBucketSize(granularity);
        var buckets = new List<DashboardTrendBucket>();

        for (var bucketFrom = range.From; bucketFrom < range.To; bucketFrom = bucketFrom.Add(bucketSize))
        {
            var bucketTo = Min(bucketFrom.Add(bucketSize), range.To);
            buckets.Add(new()
            {
                From = bucketFrom,
                To = bucketTo,
                CreatedOrStarted = await CountAsync(request.IncludeSystem, nameof(WorkflowInstance.CreatedAt), bucketFrom, bucketTo, cancellationToken),
                Finished = await CountAsync(request.IncludeSystem, nameof(WorkflowInstance.FinishedAt), bucketFrom, bucketTo, cancellationToken, subStatus: WorkflowSubStatus.Finished),
                Faulted = await CountAsync(request.IncludeSystem, nameof(WorkflowInstance.UpdatedAt), bucketFrom, bucketTo, cancellationToken, subStatus: WorkflowSubStatus.Faulted),
                Suspended = await CountAsync(request.IncludeSystem, nameof(WorkflowInstance.UpdatedAt), bucketFrom, bucketTo, cancellationToken, subStatus: WorkflowSubStatus.Suspended),
                IncidentBearing = await CountAsync(request.IncludeSystem, nameof(WorkflowInstance.UpdatedAt), bucketFrom, bucketTo, cancellationToken, hasIncidents: true)
            });
        }

        return new()
        {
            Buckets = buckets,
            AppliedRange = range.Key,
            Granularity = granularity,
            From = range.From,
            To = range.To
        };
    }

    public async Task<DashboardNeedsAttentionResponse> GetNeedsAttentionAsync(DashboardQuery query, int take, CancellationToken cancellationToken = default)
    {
        var range = rangeResolver.Resolve(query.Range);
        var overview = await GetOverviewAsync(query, cancellationToken);
        var findings = new List<DashboardFinding>();

        if (overview.Runtime.Status == DashboardRuntimeStatusKeys.Paused)
            findings.Add(Finding("runtime-paused", DashboardFindingSeverity.Warning, "Runtime is paused", "Runtime", "runtime", 10));
        else if (overview.Runtime.Status == DashboardRuntimeStatusKeys.Draining)
            findings.Add(Finding("runtime-draining", DashboardFindingSeverity.Warning, "Runtime is draining", "Runtime", "runtime", 20));

        if (overview.Runtime.FailedIngressSourceCount > 0)
            findings.Add(Finding("ingress-source-failures", DashboardFindingSeverity.Warning, $"{overview.Runtime.FailedIngressSourceCount} ingress sources need attention", "Runtime", "runtime", 30));

        if (overview.WorkflowInstances.Faulted > 0)
            findings.Add(Finding("workflow-faults", DashboardFindingSeverity.Error, $"{overview.WorkflowInstances.Faulted} workflows faulted in the selected range", "WorkflowInstances", "faulted", 40));

        if (overview.WorkflowInstances.Interrupted > 0)
            findings.Add(Finding("workflow-interrupted", DashboardFindingSeverity.Warning, $"{overview.WorkflowInstances.Interrupted} workflows were interrupted in the selected range", "WorkflowInstances", "interrupted", 50));

        if (overview.WorkflowInstances.IncidentBearing > 0)
            findings.Add(Finding("workflow-incidents", DashboardFindingSeverity.Error, $"{overview.WorkflowInstances.IncidentBearing} workflows have incidents", "WorkflowInstances", "incidents", 60));

        var structuredLogs = overview.Diagnostics.StructuredLogs;
        if (structuredLogs.StaleSourceCount > 0)
            findings.Add(Finding("structured-log-stale-sources", DashboardFindingSeverity.Warning, $"{structuredLogs.StaleSourceCount} structured log sources are stale", "StructuredLogs", "sources", 70));
        if (structuredLogs.DroppedWriteCount > 0)
            findings.Add(Finding("structured-log-dropped-writes", DashboardFindingSeverity.Error, "Structured log storage dropped writes", "StructuredLogs", "storage", 80));
        if (structuredLogs.RecentErrorOrCriticalCount > 0)
            findings.Add(Finding("structured-log-errors", DashboardFindingSeverity.Error, $"{structuredLogs.RecentErrorOrCriticalCount} error or critical structured logs were recorded", "StructuredLogs", "errors", 90));

        var consoleLogs = overview.Diagnostics.ConsoleLogs;
        if (consoleLogs.StaleSourceCount > 0)
            findings.Add(Finding("console-log-stale-sources", DashboardFindingSeverity.Warning, $"{consoleLogs.StaleSourceCount} console log sources are stale", "ConsoleLogs", "sources", 100));
        if (consoleLogs.DroppedLineCount > 0)
            findings.Add(Finding("console-log-dropped-lines", DashboardFindingSeverity.Warning, "Console log capture dropped lines", "ConsoleLogs", "dropped", 110));

        return new()
        {
            Findings = findings.OrderBy(x => x.Priority).Take(Math.Clamp(take, 1, 50)).ToList(),
            AppliedRange = range.Key
        };
    }

    public async Task<DashboardRecentActivityResponse> GetRecentActivityAsync(DashboardQuery query, int take, CancellationToken cancellationToken = default)
    {
        var range = rangeResolver.Resolve(query.Range);
        var filter = CreateRangeFilter(query.IncludeSystem, nameof(WorkflowInstance.UpdatedAt), range.From, range.To);
        var order = new WorkflowInstanceOrder<DateTimeOffset?>
        {
            KeySelector = x => x.UpdatedAt,
            Direction = OrderDirection.Descending
        };
        var page = await workflowInstanceStore.SummarizeManyAsync(filter, PageArgs.FromPage(0, Math.Clamp(take, 1, 100)), order, cancellationToken);

        return new()
        {
            Items = page.Items.Select(MapRecentActivity).ToList(),
            AppliedRange = range.Key,
            From = range.From,
            To = range.To
        };
    }

    public async Task<DashboardWorkflowHotspotsResponse> GetWorkflowHotspotsAsync(DashboardWorkflowHotspotsRequest request, CancellationToken cancellationToken = default)
    {
        var range = rangeResolver.Resolve(request.Range);
        var summaries = await workflowInstanceStore.SummarizeManyAsync(CreateRangeFilter(request.IncludeSystem, nameof(WorkflowInstance.UpdatedAt), range.From, range.To), cancellationToken);
        var metric = NormalizeHotspotMetric(request.Metric);
        var hotspots = summaries
            .GroupBy(x => x.DefinitionId)
            .Select(x => CreateHotspot(x, metric))
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.WorkflowName)
            .Take(Math.Clamp(request.Take, 1, 50))
            .ToList();

        return new()
        {
            Items = hotspots,
            AppliedRange = range.Key,
            Metric = metric,
            From = range.From,
            To = range.To
        };
    }

    private async Task<DashboardWorkflowInstanceMetrics> GetWorkflowMetricsAsync(DashboardRange range, bool includeSystem, CancellationToken cancellationToken)
    {
        var completedSummaries = (await workflowInstanceStore.SummarizeManyAsync(
            CreateRangeFilter(includeSystem, nameof(WorkflowInstance.FinishedAt), range.From, range.To, subStatus: WorkflowSubStatus.Finished),
            cancellationToken)).ToList();
        var durations = completedSummaries
            .Where(x => x.FinishedAt != null)
            .Select(x => x.FinishedAt!.Value - x.CreatedAt)
            .Where(x => x >= TimeSpan.Zero)
            .ToList();

        return new()
        {
            Running = await CountAsync(includeSystem, status: WorkflowStatus.Running, cancellationToken: cancellationToken),
            Completed = completedSummaries.Count,
            Faulted = await CountAsync(includeSystem, nameof(WorkflowInstance.UpdatedAt), range.From, range.To, cancellationToken, subStatus: WorkflowSubStatus.Faulted),
            Suspended = await CountAsync(includeSystem, subStatus: WorkflowSubStatus.Suspended, cancellationToken: cancellationToken),
            Interrupted = await CountAsync(includeSystem, nameof(WorkflowInstance.UpdatedAt), range.From, range.To, cancellationToken, subStatus: WorkflowSubStatus.Interrupted),
            IncidentBearing = await CountAsync(includeSystem, hasIncidents: true, cancellationToken: cancellationToken),
            AverageDuration = durations.Count == 0 ? null : TimeSpan.FromTicks(Convert.ToInt64(durations.Average(x => x.Ticks)))
        };
    }

    private DashboardRuntimeStatus GetRuntimeStatus()
    {
        var status = runtimeAdminService.GetStatus();
        var state = status.State;
        var runtimeStatus = state.IsAcceptingNewWork
            ? DashboardRuntimeStatusKeys.AcceptingWork
            : state.DrainStartedAt != null
                ? DashboardRuntimeStatusKeys.Draining
                : DashboardRuntimeStatusKeys.Paused;
        var failedSourceCount = status.Sources.Count(x => x.LastError != null);

        return new()
        {
            Status = runtimeStatus,
            IsAcceptingWork = state.IsAcceptingNewWork,
            ActiveExecutionCycleCount = status.ActiveExecutionCycleCount,
            IngressSourceCount = status.Sources.Count,
            FailedIngressSourceCount = failedSourceCount,
            PausedAt = state.PausedAt,
            DrainStartedAt = state.DrainStartedAt,
            Reason = state.Reason.ToString()
        };
    }

    private async Task<DashboardDiagnosticsSummary> GetDiagnosticsSummaryAsync(DashboardRange range, CancellationToken cancellationToken)
    {
        var structuredLogs = await GetStructuredLogSummaryAsync(range, cancellationToken);
        var consoleLogs = await GetConsoleLogSummaryAsync(range, cancellationToken);
        return new()
        {
            StructuredLogs = structuredLogs,
            ConsoleLogs = consoleLogs
        };
    }

    private async Task<DashboardStructuredLogSummary> GetStructuredLogSummaryAsync(DashboardRange range, CancellationToken cancellationToken)
    {
        var provider = serviceProvider.GetService<IStructuredLogProvider>();
        if (provider == null)
            return new();

        try
        {
            var sources = await provider.ListSourcesAsync(cancellationToken);
            var storageDiagnostics = serviceProvider.GetServices<IStructuredLogStorageDiagnostics>().ToList();
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

    private async Task<DashboardConsoleLogSummary> GetConsoleLogSummaryAsync(DashboardRange range, CancellationToken cancellationToken)
    {
        var provider = serviceProvider.GetService<IConsoleLogProvider>();
        if (provider == null)
            return new();

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

    private async Task<long> CountAsync(
        bool includeSystem,
        string? timestampColumn = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default,
        WorkflowStatus? status = null,
        WorkflowSubStatus? subStatus = null,
        bool? hasIncidents = null)
    {
        return await workflowInstanceStore.CountAsync(CreateRangeFilter(includeSystem, timestampColumn, from, to, status, subStatus, hasIncidents), cancellationToken);
    }

    private static WorkflowInstanceFilter CreateRangeFilter(
        bool includeSystem,
        string? timestampColumn,
        DateTimeOffset? from,
        DateTimeOffset? to,
        WorkflowStatus? status = null,
        WorkflowSubStatus? subStatus = null,
        bool? hasIncidents = null)
    {
        var timestampFilters = new List<TimestampFilter>();
        if (timestampColumn != null && from != null)
            timestampFilters.Add(new() { Column = timestampColumn, Operator = TimestampFilterOperator.GreaterThanOrEqual, Timestamp = from.Value });
        if (timestampColumn != null && to != null)
            timestampFilters.Add(new() { Column = timestampColumn, Operator = TimestampFilterOperator.LessThan, Timestamp = to.Value });

        return new()
        {
            IsSystem = includeSystem ? null : false,
            WorkflowStatus = status,
            WorkflowSubStatus = subStatus,
            HasIncidents = hasIncidents,
            TimestampFilters = timestampFilters.Count == 0 ? null : timestampFilters
        };
    }

    private static DashboardRecentActivityItem MapRecentActivity(WorkflowInstanceSummary summary) => new()
    {
        InstanceId = summary.Id,
        DefinitionId = summary.DefinitionId,
        WorkflowName = summary.Name,
        Status = summary.Status.ToString(),
        SubStatus = summary.SubStatus.ToString(),
        IncidentCount = summary.IncidentCount,
        Duration = summary.FinishedAt == null ? null : summary.FinishedAt.Value - summary.CreatedAt,
        CreatedAt = summary.CreatedAt,
        UpdatedAt = summary.UpdatedAt,
        FinishedAt = summary.FinishedAt
    };

    private static DashboardHotspot CreateHotspot(IGrouping<string, WorkflowInstanceSummary> group, string metric)
    {
        var items = group.ToList();
        var durations = items
            .Where(x => x.FinishedAt != null)
            .Select(x => x.FinishedAt!.Value - x.CreatedAt)
            .Where(x => x >= TimeSpan.Zero)
            .ToList();
        var value = metric switch
        {
            DashboardHotspotMetric.Executions => items.Count,
            DashboardHotspotMetric.Incidents => items.Sum(x => x.IncidentCount),
            DashboardHotspotMetric.Duration => durations.Count == 0 ? 0 : Convert.ToInt64(durations.Average(x => x.TotalMilliseconds)),
            _ => items.LongCount(x => x.SubStatus == WorkflowSubStatus.Faulted)
        };

        return new()
        {
            DefinitionId = group.Key,
            WorkflowName = items.Select(x => x.Name).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
            Value = value,
            AverageDuration = durations.Count == 0 ? null : TimeSpan.FromTicks(Convert.ToInt64(durations.Average(x => x.Ticks)))
        };
    }

    private static string NormalizeHotspotMetric(string? metric) =>
        metric?.Trim().ToLowerInvariant() switch
        {
            "executions" => DashboardHotspotMetric.Executions,
            "incidents" => DashboardHotspotMetric.Incidents,
            "duration" => DashboardHotspotMetric.Duration,
            _ => DashboardHotspotMetric.Faults
        };

    private static DashboardFinding Finding(string id, string severity, string message, string? targetKind, string? target, int priority) => new()
    {
        Id = id,
        Severity = severity,
        Message = message,
        TargetKind = targetKind,
        Target = target,
        Priority = priority
    };

    private static DateTimeOffset Min(DateTimeOffset left, DateTimeOffset right) => left <= right ? left : right;
}
