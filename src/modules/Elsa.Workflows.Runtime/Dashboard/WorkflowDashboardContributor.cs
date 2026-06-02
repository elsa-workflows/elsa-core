using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Dashboard.Abstractions.Contracts;
using Elsa.Dashboard.Abstractions.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Enums;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Runtime.Dashboard;

public class WorkflowDashboardContributor(
    IWorkflowInstanceStore workflowInstanceStore,
    IWorkflowRuntimeAdminService runtimeAdminService) : IDashboardContributor
{
    public string Id => "workflows";

    public int Order => 100;

    public async ValueTask<DashboardOverviewContribution?> GetOverviewAsync(DashboardContext context)
    {
        return new()
        {
            Runtime = GetRuntimeStatus(),
            WorkflowInstances = await GetWorkflowMetricsAsync(context.Range, context.IncludeSystem, context.CancellationToken)
        };
    }

    public async ValueTask<IReadOnlyCollection<DashboardFinding>> GetFindingsAsync(DashboardContext context)
    {
        var runtime = GetRuntimeStatus();
        var workflowMetrics = await GetWorkflowMetricsAsync(context.Range, context.IncludeSystem, context.CancellationToken);
        var findings = new List<DashboardFinding>();

        if (runtime.Status == DashboardRuntimeStatusKeys.Paused)
            findings.Add(Finding("runtime-paused", DashboardFindingSeverity.Warning, "Runtime is paused", "Runtime", "runtime", 10));
        else if (runtime.Status == DashboardRuntimeStatusKeys.Draining)
            findings.Add(Finding("runtime-draining", DashboardFindingSeverity.Warning, "Runtime is draining", "Runtime", "runtime", 20));

        if (runtime.FailedIngressSourceCount > 0)
            findings.Add(Finding("ingress-source-failures", DashboardFindingSeverity.Warning, $"{runtime.FailedIngressSourceCount} ingress sources need attention", "Runtime", "runtime", 30));

        if (workflowMetrics.Faulted > 0)
            findings.Add(Finding("workflow-faults", DashboardFindingSeverity.Error, $"{workflowMetrics.Faulted} workflows faulted in the selected range", "WorkflowInstances", "faulted", 40));

        if (workflowMetrics.Interrupted > 0)
            findings.Add(Finding("workflow-interrupted", DashboardFindingSeverity.Warning, $"{workflowMetrics.Interrupted} workflows were interrupted in the selected range", "WorkflowInstances", "interrupted", 50));

        if (workflowMetrics.IncidentBearing > 0)
            findings.Add(Finding("workflow-incidents", DashboardFindingSeverity.Error, $"{workflowMetrics.IncidentBearing} workflows have incidents", "WorkflowInstances", "incidents", 60));

        return findings;
    }

    public async ValueTask<DashboardTrendResponse?> GetWorkflowTrendsAsync(DashboardTrendContext context)
    {
        var bucketSize = GetBucketSize(context.Granularity);
        var buckets = new List<DashboardTrendBucket>();

        for (var bucketFrom = context.Range.From; bucketFrom < context.Range.To; bucketFrom = bucketFrom.Add(bucketSize))
        {
            var bucketTo = Min(bucketFrom.Add(bucketSize), context.Range.To);
            buckets.Add(new()
            {
                From = bucketFrom,
                To = bucketTo,
                CreatedOrStarted = await CountAsync(context.IncludeSystem, nameof(WorkflowInstance.CreatedAt), bucketFrom, bucketTo, context.CancellationToken),
                Finished = await CountAsync(context.IncludeSystem, nameof(WorkflowInstance.FinishedAt), bucketFrom, bucketTo, context.CancellationToken, subStatus: WorkflowSubStatus.Finished),
                Faulted = await CountAsync(context.IncludeSystem, nameof(WorkflowInstance.UpdatedAt), bucketFrom, bucketTo, context.CancellationToken, subStatus: WorkflowSubStatus.Faulted),
                Suspended = await CountAsync(context.IncludeSystem, nameof(WorkflowInstance.UpdatedAt), bucketFrom, bucketTo, context.CancellationToken, subStatus: WorkflowSubStatus.Suspended),
                IncidentBearing = await CountAsync(context.IncludeSystem, nameof(WorkflowInstance.UpdatedAt), bucketFrom, bucketTo, context.CancellationToken, hasIncidents: true)
            });
        }

        return new()
        {
            Buckets = buckets,
            AppliedRange = context.Range.Key,
            Granularity = context.Granularity,
            From = context.Range.From,
            To = context.Range.To
        };
    }

    public async ValueTask<DashboardRecentActivityResponse?> GetRecentActivityAsync(DashboardListContext context)
    {
        var filter = CreateRangeFilter(context.IncludeSystem, nameof(WorkflowInstance.UpdatedAt), context.Range.From, context.Range.To);
        var order = new WorkflowInstanceOrder<DateTimeOffset?>
        {
            KeySelector = x => x.UpdatedAt,
            Direction = OrderDirection.Descending
        };
        var page = await workflowInstanceStore.SummarizeManyAsync(filter, PageArgs.FromPage(0, context.Take), order, context.CancellationToken);

        return new()
        {
            Items = page.Items.Select(MapRecentActivity).ToList(),
            AppliedRange = context.Range.Key,
            From = context.Range.From,
            To = context.Range.To
        };
    }

    public async ValueTask<DashboardWorkflowHotspotsResponse?> GetWorkflowHotspotsAsync(DashboardHotspotsContext context)
    {
        var summaries = await workflowInstanceStore.SummarizeManyAsync(CreateRangeFilter(context.IncludeSystem, nameof(WorkflowInstance.UpdatedAt), context.Range.From, context.Range.To), context.CancellationToken);
        var hotspots = summaries
            .GroupBy(x => x.DefinitionId)
            .Select(x => CreateHotspot(x, context.Metric))
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.WorkflowName)
            .Take(context.Take)
            .ToList();

        return new()
        {
            Items = hotspots,
            AppliedRange = context.Range.Key,
            Metric = context.Metric,
            From = context.Range.From,
            To = context.Range.To
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

    private static TimeSpan GetBucketSize(string granularity) =>
        granularity.Equals(DashboardTrendGranularity.Minute, StringComparison.OrdinalIgnoreCase)
            ? TimeSpan.FromMinutes(1)
            : granularity.Equals(DashboardTrendGranularity.Day, StringComparison.OrdinalIgnoreCase)
                ? TimeSpan.FromDays(1)
                : TimeSpan.FromHours(1);

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
