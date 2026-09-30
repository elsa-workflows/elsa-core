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
        var canRead = DashboardAccess.WithDefault(query.CanRead);
        var contributions = new List<(DashboardOverviewPermissions? Declared, DashboardOverviewContribution Contribution)>();

        var readable = ReadableContributors(canRead, out var skipped);

        foreach (var contributor in readable)
        {
            var contribution = await ExecuteContributorAsync(contributor, x => x.GetOverviewAsync(context).AsTask(), cancellationToken);
            if (contribution != null)
                contributions.Add((contributor.OverviewPermissions, contribution));
        }

        var unauthorized = DashboardCapabilityStatus.Unauthorized;
        var runtime = MergeSection(contributions, skipped, canRead, x => x.Runtime, x => x.Runtime, MergeRuntime, new() { Capability = unauthorized });
        var workflowInstances = MergeSection(contributions, skipped, canRead, x => x.WorkflowInstances, x => x.WorkflowInstances, MergeWorkflowMetrics, new() { Capability = unauthorized });
        var structuredLogs = MergeSection(contributions, skipped, canRead, x => Installed(x.Diagnostics?.StructuredLogs, y => y.Capability), x => x.StructuredLogs, MergeStructuredLogs, new() { Capability = unauthorized });
        var consoleLogs = MergeSection(contributions, skipped, canRead, x => Installed(x.Diagnostics?.ConsoleLogs, y => y.Capability), x => x.ConsoleLogs, MergeConsoleLogs, new() { Capability = unauthorized });
        var metrics = contributions.SelectMany(x => x.Contribution.Metrics).Where(x => canRead(x.Permission)).OrderBy(x => x.Order).ThenBy(x => x.Id, StringComparer.Ordinal).ToList();
        var panels = contributions.SelectMany(x => x.Contribution.Panels).Where(x => canRead(x.Permission)).OrderBy(x => x.Order).ThenBy(x => x.Id, StringComparer.Ordinal).ToList();

        // A caller who may read nothing learns nothing about the deployment either; the range is the caller's own.
        var withheld = new[] { runtime.Capability, workflowInstances.Capability, structuredLogs.Capability, consoleLogs.Capability }.All(x => x.Status == unauthorized.Status)
                       && metrics.Count == 0 && panels.Count == 0;

        return new()
        {
            BackendName = withheld ? null : environment.ApplicationName,
            EnvironmentName = withheld ? null : environment.EnvironmentName,
            Runtime = runtime,
            WorkflowInstances = workflowInstances,
            Diagnostics = new()
            {
                StructuredLogs = structuredLogs,
                ConsoleLogs = consoleLogs
            },
            Metrics = metrics,
            Panels = panels,
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
        var canRead = DashboardAccess.WithDefault(request.CanRead);
        var responses = (await CollectAsync(canRead, contributor => contributor.GetWorkflowTrendsAsync(context).AsTask(), cancellationToken))
            .Where(x => canRead(x.Permission))
            .ToList();
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
        var canRead = DashboardAccess.WithDefault(query.CanRead);
        var findings = (await CollectManyAsync(canRead, contributor => contributor.GetFindingsAsync(context).AsTask(), cancellationToken))
            .Where(x => canRead(x.Permission));

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
        var canRead = DashboardAccess.WithDefault(query.CanRead);
        var responses = (await CollectAsync(canRead, contributor => contributor.GetRecentActivityAsync(context).AsTask(), cancellationToken))
            .Where(x => canRead(x.Permission))
            .ToList();
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
        var canRead = DashboardAccess.WithDefault(request.CanRead);
        var responses = (await CollectAsync(canRead, contributor => contributor.GetWorkflowHotspotsAsync(context).AsTask(), cancellationToken))
            .Where(x => canRead(x.Permission))
            .ToList();
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

    /// <summary>
    /// The contributors worth invoking for this caller, in order. A contributor that declared its permissions and whose
    /// permissions the caller may all not read has nothing the caller can see, so it is not invoked; it is returned in
    /// <paramref name="skipped"/> so the sections it declared can still be reported as withheld.
    /// </summary>
    private IReadOnlyCollection<IDashboardContributor> ReadableContributors(Func<DashboardPermission?, bool> canRead, out IReadOnlyCollection<DashboardOverviewPermissions> skipped)
    {
        var readable = new List<IDashboardContributor>();
        var skippedPermissions = new List<DashboardOverviewPermissions>();

        foreach (var contributor in OrderedContributors)
        {
            var declared = contributor.OverviewPermissions?.All().ToList();

            if (declared is { Count: > 0 } && !declared.Any(x => canRead(x)))
                skippedPermissions.Add(contributor.OverviewPermissions!);
            else
                readable.Add(contributor);
        }

        skipped = skippedPermissions;
        return readable;
    }

    private IReadOnlyCollection<IDashboardContributor> ReadableContributors(Func<DashboardPermission?, bool> canRead) => ReadableContributors(canRead, out _);

    private async Task<IReadOnlyCollection<T>> CollectAsync<T>(
        Func<DashboardPermission?, bool> canRead,
        Func<IDashboardContributor, Task<T?>> action,
        CancellationToken cancellationToken)
        where T : class
    {
        var results = new List<T>();
        foreach (var contributor in ReadableContributors(canRead))
        {
            var result = await ExecuteContributorAsync(contributor, action, cancellationToken);
            if (result != null)
                results.Add(result);
        }

        return results;
    }

    private async Task<IReadOnlyCollection<T>> CollectManyAsync<T>(
        Func<DashboardPermission?, bool> canRead,
        Func<IDashboardContributor, Task<IReadOnlyCollection<T>>> action,
        CancellationToken cancellationToken)
    {
        var results = new List<T>();
        foreach (var contributor in ReadableContributors(canRead))
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

    /// <summary>
    /// Merges the contributions of one overview section the caller may read. A section some contributor supplied, or
    /// declared and was skipped for, but the caller may not read at all is <paramref name="unauthorized"/>, never the
    /// default, so a denied section is told apart from one nobody supplied. A section supplied without a declared
    /// permission needs <c>dashboard:view</c>.
    /// </summary>
    private static T MergeSection<T>(
        IEnumerable<(DashboardOverviewPermissions? Declared, DashboardOverviewContribution Contribution)> contributions,
        IReadOnlyCollection<DashboardOverviewPermissions> skipped,
        Func<DashboardPermission?, bool> canRead,
        Func<DashboardOverviewContribution, T?> select,
        Func<DashboardOverviewPermissions, DashboardPermission?> permissionOf,
        Func<IReadOnlyCollection<T>, T> merge,
        T unauthorized)
        where T : class
    {
        var supplied = contributions
            .Select(x => (Section: select(x.Contribution), Permission: x.Declared == null ? null : permissionOf(x.Declared)))
            .Where(x => x.Section != null)
            .ToList();
        var readable = supplied.Where(x => canRead(x.Permission)).Select(x => x.Section!).ToList();
        var skippedSupply = skipped.Any(x => permissionOf(x) != null);

        return readable.Count == 0 && (supplied.Count > 0 || skippedSupply) ? unauthorized : merge(readable);
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
