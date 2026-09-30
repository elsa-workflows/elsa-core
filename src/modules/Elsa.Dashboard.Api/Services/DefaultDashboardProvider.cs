using Elsa.Dashboard.Abstractions.Contracts;
using Elsa.Dashboard.Abstractions.Models;
using Elsa.Dashboard.Api.Authorization;
using Microsoft.Extensions.Hosting;

namespace Elsa.Dashboard.Api.Services;

public class DefaultDashboardProvider(
    IEnumerable<IDashboardContributor> contributors,
    DashboardRangeResolver rangeResolver,
    IHostEnvironment environment) : IDashboardProvider
{
    public async Task<DashboardOverview> GetOverviewAsync(DashboardQuery query, CancellationToken cancellationToken = default)
    {
        var range = rangeResolver.Resolve(query.Range);
        var context = CreateContext(range, query.IncludeSystem, cancellationToken);
        var canRead = CanRead(query);
        var contributions = new List<DashboardOverviewContribution>();

        foreach (var contributor in OrderedContributors)
        {
            var contribution = await ExecuteContributorAsync(contributor, x => x.GetOverviewAsync(context).AsTask(), cancellationToken);
            if (contribution != null)
                contributions.Add(contribution);
        }

        return new()
        {
            BackendName = environment.ApplicationName,
            EnvironmentName = environment.EnvironmentName,
            Runtime = MergeSection(contributions, canRead, x => x.Runtime, x => x.Runtime, MergeRuntime, new() { Capability = DashboardCapabilityStatus.Unauthorized }),
            WorkflowInstances = MergeSection(contributions, canRead, x => x.WorkflowInstances, x => x.WorkflowInstances, MergeWorkflowMetrics, new() { Capability = DashboardCapabilityStatus.Unauthorized }),
            Diagnostics = new()
            {
                StructuredLogs = MergeSection(contributions, canRead, x => Installed(x.Diagnostics?.StructuredLogs, y => y.Capability), x => x.StructuredLogs, MergeStructuredLogs, new() { Capability = DashboardCapabilityStatus.Unauthorized }),
                ConsoleLogs = MergeSection(contributions, canRead, x => Installed(x.Diagnostics?.ConsoleLogs, y => y.Capability), x => x.ConsoleLogs, MergeConsoleLogs, new() { Capability = DashboardCapabilityStatus.Unauthorized })
            },
            Metrics = contributions.SelectMany(x => x.Metrics).Where(x => canRead(x.Permission ?? DashboardAccess.Overview)).OrderBy(x => x.Order).ThenBy(x => x.Id, StringComparer.Ordinal).ToList(),
            Panels = contributions.SelectMany(x => x.Panels).Where(x => canRead(x.Permission ?? DashboardAccess.Overview)).OrderBy(x => x.Order).ThenBy(x => x.Id, StringComparer.Ordinal).ToList(),
            AppliedRange = range.Key,
            From = range.From,
            To = range.To
        };
    }

    public async Task<DashboardTrendResponse> GetWorkflowTrendsAsync(DashboardTrendRequest request, CancellationToken cancellationToken = default)
    {
        var range = rangeResolver.Resolve(request.Range);
        var granularity = rangeResolver.ResolveGranularity(request.Granularity, range.Key);
        var context = new DashboardTrendContext(range, granularity, request.IncludeSystem, cancellationToken, EnvironmentName: environment.EnvironmentName);
        var responses = await CollectAsync(contributor => contributor.GetWorkflowTrendsAsync(context).AsTask(), cancellationToken);
        var buckets = responses
            .SelectMany(x => x.Buckets)
            .GroupBy(x => new { x.From, x.To })
            .Select(x => new DashboardTrendBucket
            {
                From = x.Key.From,
                To = x.Key.To,
                CreatedOrStarted = x.Sum(y => y.CreatedOrStarted),
                Finished = x.Sum(y => y.Finished),
                Faulted = x.Sum(y => y.Faulted),
                Suspended = x.Sum(y => y.Suspended),
                IncidentBearing = x.Sum(y => y.IncidentBearing)
            })
            .OrderBy(x => x.From)
            .ToList();

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
        var context = CreateContext(range, query.IncludeSystem, cancellationToken);
        var canRead = CanRead(query);
        var findings = (await CollectManyAsync(contributor => contributor.GetFindingsAsync(context).AsTask(), cancellationToken))
            .Where(x => canRead(x.Permission ?? DashboardAccess.Overview));

        return new()
        {
            Findings = findings
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.Id, StringComparer.Ordinal)
                .Take(Math.Clamp(take, 1, 50))
                .ToList(),
            AppliedRange = range.Key
        };
    }

    public async Task<DashboardRecentActivityResponse> GetRecentActivityAsync(DashboardQuery query, int take, CancellationToken cancellationToken = default)
    {
        var range = rangeResolver.Resolve(query.Range);
        var context = new DashboardListContext(range, Math.Clamp(take, 1, 100), query.IncludeSystem, cancellationToken, EnvironmentName: environment.EnvironmentName);
        var responses = await CollectAsync(contributor => contributor.GetRecentActivityAsync(context).AsTask(), cancellationToken);
        var items = responses
            .SelectMany(x => x.Items)
            .OrderByDescending(x => x.UpdatedAt ?? x.FinishedAt ?? x.CreatedAt)
            .ThenBy(x => x.InstanceId, StringComparer.Ordinal)
            .Take(context.Take)
            .ToList();

        return new()
        {
            Items = items,
            AppliedRange = range.Key,
            From = range.From,
            To = range.To
        };
    }

    public async Task<DashboardWorkflowHotspotsResponse> GetWorkflowHotspotsAsync(DashboardWorkflowHotspotsRequest request, CancellationToken cancellationToken = default)
    {
        var range = rangeResolver.Resolve(request.Range);
        var metric = NormalizeHotspotMetric(request.Metric);
        var take = Math.Clamp(request.Take, 1, 50);
        var context = new DashboardHotspotsContext(range, metric, take, request.IncludeSystem, cancellationToken, EnvironmentName: environment.EnvironmentName);
        var responses = await CollectAsync(contributor => contributor.GetWorkflowHotspotsAsync(context).AsTask(), cancellationToken);
        var items = responses
            .SelectMany(x => x.Items)
            .GroupBy(x => x.DefinitionId)
            .Select(x => new DashboardHotspot
            {
                DefinitionId = x.Key,
                WorkflowName = x.Select(y => y.WorkflowName).FirstOrDefault(y => !string.IsNullOrWhiteSpace(y)),
                Value = x.Sum(y => y.Value),
                AverageDuration = AverageDuration(x.Select(y => y.AverageDuration))
            })
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.WorkflowName, StringComparer.Ordinal)
            .Take(take)
            .ToList();

        return new()
        {
            Items = items,
            AppliedRange = range.Key,
            Metric = metric,
            From = range.From,
            To = range.To
        };
    }

    private IReadOnlyCollection<IDashboardContributor> OrderedContributors =>
        contributors
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .ToList();

    private DashboardContext CreateContext(DashboardRange range, bool includeSystem, CancellationToken cancellationToken) =>
        new(range, includeSystem, cancellationToken, EnvironmentName: environment.EnvironmentName);

    private async Task<IReadOnlyCollection<T>> CollectAsync<T>(
        Func<IDashboardContributor, Task<T?>> action,
        CancellationToken cancellationToken)
        where T : class
    {
        var results = new List<T>();
        foreach (var contributor in OrderedContributors)
        {
            var result = await ExecuteContributorAsync(contributor, action, cancellationToken);
            if (result != null)
                results.Add(result);
        }

        return results;
    }

    private async Task<IReadOnlyCollection<T>> CollectManyAsync<T>(
        Func<IDashboardContributor, Task<IReadOnlyCollection<T>>> action,
        CancellationToken cancellationToken)
    {
        var results = new List<T>();
        foreach (var contributor in OrderedContributors)
        {
            var result = await ExecuteContributorAsync(contributor, action, cancellationToken);
            if (result != null)
                results.AddRange(result);
        }

        return results;
    }

    private static async Task<T?> ExecuteContributorAsync<T>(
        IDashboardContributor contributor,
        Func<IDashboardContributor, Task<T>> action,
        CancellationToken cancellationToken)
    {
        try
        {
            return await action(contributor);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return default;
        }
    }

    private static Func<DashboardPermission, bool> CanRead(DashboardQuery query) => query.CanRead ?? (_ => true);

    /// <summary>
    /// Merges the contributions of one overview section the caller may read. A section some contribution supplied but the
    /// caller may not read at all is <paramref name="unauthorized"/>, never the default, so a denied section is told
    /// apart from one nobody supplied. A section supplied without a declared permission needs <c>dashboard:view</c>.
    /// </summary>
    private static T MergeSection<T>(
        IEnumerable<DashboardOverviewContribution> contributions,
        Func<DashboardPermission, bool> canRead,
        Func<DashboardOverviewContribution, T?> select,
        Func<DashboardOverviewPermissions, DashboardPermission?> permissionOf,
        Func<IReadOnlyCollection<T>, T> merge,
        T unauthorized)
        where T : class
    {
        var supplied = contributions
            .Select(x => (Section: select(x), Permission: permissionOf(x.Permissions) ?? DashboardAccess.Overview))
            .Where(x => x.Section != null)
            .ToList();
        var readable = supplied.Where(x => canRead(x.Permission)).Select(x => x.Section!).ToList();

        return supplied.Count > 0 && readable.Count == 0 ? unauthorized : merge(readable);
    }

    // A diagnostics slice that reports NotInstalled was not supplied: every contribution carries both slices.
    private static T? Installed<T>(T? section, Func<T, DashboardCapabilityStatus> capability) where T : class =>
        section != null && capability(section).Status != DashboardCapabilityStatus.NotInstalled.Status ? section : null;

    private static DashboardRuntimeStatus MergeRuntime(IReadOnlyCollection<DashboardRuntimeStatus> runtimes) =>
        runtimes.FirstOrDefault(x => x.Status != DashboardRuntimeStatusKeys.Unavailable) ?? new();

    private static DashboardWorkflowInstanceMetrics MergeWorkflowMetrics(IReadOnlyCollection<DashboardWorkflowInstanceMetrics> metrics) =>
        new()
        {
            Running = metrics.Sum(x => x.Running),
            Completed = metrics.Sum(x => x.Completed),
            Faulted = metrics.Sum(x => x.Faulted),
            Suspended = metrics.Sum(x => x.Suspended),
            Interrupted = metrics.Sum(x => x.Interrupted),
            IncidentBearing = metrics.Sum(x => x.IncidentBearing),
            AverageDuration = AverageDuration(metrics.Select(x => x.AverageDuration))
        };

    private static DashboardStructuredLogSummary MergeStructuredLogs(IReadOnlyCollection<DashboardStructuredLogSummary> summaries) =>
        summaries.FirstOrDefault() ?? new();

    private static DashboardConsoleLogSummary MergeConsoleLogs(IReadOnlyCollection<DashboardConsoleLogSummary> summaries) =>
        summaries.FirstOrDefault() ?? new();

    private static TimeSpan? AverageDuration(IEnumerable<TimeSpan?> durations)
    {
        var values = durations.OfType<TimeSpan>().Where(x => x >= TimeSpan.Zero).ToList();
        return values.Count == 0 ? null : TimeSpan.FromTicks(Convert.ToInt64(values.Average(x => x.Ticks)));
    }

    private static string NormalizeHotspotMetric(string? metric) =>
        metric?.Trim().ToLowerInvariant() switch
        {
            "executions" => DashboardHotspotMetric.Executions,
            "incidents" => DashboardHotspotMetric.Incidents,
            "duration" => DashboardHotspotMetric.Duration,
            _ => DashboardHotspotMetric.Faults
        };
}
