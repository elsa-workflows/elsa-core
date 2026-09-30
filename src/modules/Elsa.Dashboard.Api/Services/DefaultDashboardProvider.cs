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
        var failed = new List<DashboardOverviewPermissions>();

        var readable = ReadableContributors(canRead, out var skipped, out var declaredPermissions);

        foreach (var contributor in readable)
        {
            var contribution = await ExecuteContributorAsync(contributor, x => x.GetOverviewAsync(context).AsTask(), cancellationToken);
            if (contribution != null)
                contributions.Add((contributor.OverviewPermissions, contribution));
            else if (contributor.OverviewPermissions != null)
                failed.Add(contributor.OverviewPermissions);
        }

        var unauthorized = DashboardCapabilityStatus.Unauthorized;
        var unavailable = new DashboardCapabilityStatus(DashboardCapabilityStatus.Unavailable.Status, "Some sources are unavailable; figures would be incomplete");
        var runtime = MergeSection(contributions, skipped, failed, canRead, x => x.Runtime, x => x.Runtime, x => x.Capability.Status == unavailable.Status || x.Status == DashboardRuntimeStatusKeys.Unavailable, MergeRuntime, new() { Capability = unauthorized }, new() { Capability = unavailable });
        var workflowInstances = MergeSection(contributions, skipped, failed, canRead, x => x.WorkflowInstances, x => x.WorkflowInstances, x => x.Capability.Status == unavailable.Status, MergeWorkflowMetrics, new() { Capability = unauthorized }, new() { Capability = unavailable });
        var structuredLogs = MergeSection(contributions, skipped, failed, canRead, x => Installed(x.Diagnostics?.StructuredLogs, y => y.Capability), x => x.StructuredLogs, x => x.Capability.Status == unavailable.Status, summaries => summaries.FirstOrDefault() ?? new(), new() { Capability = unauthorized }, new() { Capability = unavailable });
        var consoleLogs = MergeSection(contributions, skipped, failed, canRead, x => Installed(x.Diagnostics?.ConsoleLogs, y => y.Capability), x => x.ConsoleLogs, x => x.Capability.Status == unavailable.Status, summaries => summaries.FirstOrDefault() ?? new(), new() { Capability = unauthorized }, new() { Capability = unavailable });
        var metrics = contributions.SelectMany(x => x.Contribution.Metrics).Where(x => canRead(x.Permission)).OrderBy(x => x.Order).ThenBy(x => x.Id, StringComparer.Ordinal).ToList();
        var panels = contributions.SelectMany(x => x.Contribution.Panels).Where(x => canRead(x.Permission)).OrderBy(x => x.Order).ThenBy(x => x.Id, StringComparer.Ordinal).ToList();

        // A caller whose permissions read nothing the contributors declared or supplied, and not the whole overview
        // either, learns nothing about the deployment: no names, and no hint of which modules are installed. This
        // follows the caller's permissions, never what happened to come back, so a contributor that failed for a caller
        // who may read its data makes that section Unavailable, not the caller one who reads nothing. The range is the caller's own.
        var suppliedPermissions = contributions.SelectMany(x => x.Contribution.Metrics.Select(y => y.Permission).Concat(x.Contribution.Panels.Select(y => y.Permission)));
        var withheld = !canRead(null) && !declaredPermissions.Cast<DashboardPermission?>().Concat(suppliedPermissions).Any(canRead);
        T Section<T>(OverviewSection<T> section) => withheld ? section.Denied : section.Value;

        return new()
        {
            BackendName = withheld ? null : environment.ApplicationName,
            EnvironmentName = withheld ? null : environment.EnvironmentName,
            Runtime = Section(runtime),
            WorkflowInstances = Section(workflowInstances),
            Diagnostics = new()
            {
                StructuredLogs = Section(structuredLogs),
                ConsoleLogs = Section(consoleLogs)
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
        var responses = (await CollectAsync(contributor => contributor.GetWorkflowTrendsAsync(context).AsTask(), cancellationToken))
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
        var findings = (await CollectManyAsync(contributor => contributor.GetFindingsAsync(context).AsTask(), cancellationToken))
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
        var responses = (await CollectAsync(contributor => contributor.GetRecentActivityAsync(context).AsTask(), cancellationToken))
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
        var responses = (await CollectAsync(contributor => contributor.GetWorkflowHotspotsAsync(context).AsTask(), cancellationToken))
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
    /// The contributors worth invoking for the overview, in order. A contributor that declared its overview permissions
    /// (those of everything it adds: sections, metric cards and panels) and whose permissions the caller may all not
    /// read has nothing the caller can see in the overview, so it is not invoked; it is returned in <paramref name="skipped"/> so the sections it declared can still be reported as withheld.
    /// Only the overview skips: every other call invokes every contributor and filters afterwards.
    /// </summary>
    private IReadOnlyCollection<IDashboardContributor> ReadableContributors(
        Func<DashboardPermission?, bool> canRead,
        out IReadOnlyCollection<DashboardOverviewPermissions> skipped,
        out IReadOnlyCollection<DashboardPermission> declaredPermissions)
    {
        var readable = new List<IDashboardContributor>();
        var skippedPermissions = new List<DashboardOverviewPermissions>();
        var allDeclared = new List<DashboardPermission>();

        foreach (var contributor in OrderedContributors)
        {
            var declared = contributor.OverviewPermissions?.All().ToList();
            allDeclared.AddRange(declared ?? []);

            if (declared is { Count: > 0 } && !declared.Any(x => canRead(x)))
                skippedPermissions.Add(contributor.OverviewPermissions!);
            else
                readable.Add(contributor);
        }

        skipped = skippedPermissions;
        declaredPermissions = allDeclared;
        return readable;
    }

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

    /// <summary>
    /// Merges the contributions to one overview section by one rule, per contribution. Contributions the caller may not
    /// read are ignored. If any contribution the caller may read failed (it reported <paramref name="isFailed"/>, or its
    /// contributor declared the section and returned nothing), the section is <paramref name="unavailable"/> with no
    /// figures, so partial totals are never presented as complete. Otherwise the readable contributions are merged. Only
    /// when the caller may read none of them is the section <paramref name="denied"/>, which tells a denied section apart
    /// from one nobody supplied. A section supplied without a declared permission needs <c>dashboard:view</c>.
    /// </summary>
    private static OverviewSection<T> MergeSection<T>(
        IEnumerable<(DashboardOverviewPermissions? Declared, DashboardOverviewContribution Contribution)> contributions,
        IReadOnlyCollection<DashboardOverviewPermissions> skipped,
        IReadOnlyCollection<DashboardOverviewPermissions> failed,
        Func<DashboardPermission?, bool> canRead,
        Func<DashboardOverviewContribution, T?> select,
        Func<DashboardOverviewPermissions, DashboardPermission?> permissionOf,
        Func<T, bool> isFailed,
        Func<IReadOnlyCollection<T>, T> merge,
        T denied,
        T unavailable)
        where T : class
    {
        var supplied = contributions
            .Select(x => (Section: select(x.Contribution), Permission: x.Declared == null ? null : permissionOf(x.Declared)))
            .Where(x => x.Section != null)
            .ToList();
        var readable = supplied.Where(x => canRead(x.Permission)).Select(x => x.Section!).ToList();
        var readableFailure = readable.Any(isFailed) || failed.Any(x => permissionOf(x) is { } permission && canRead(permission));

        if (readableFailure)
            return new(unavailable, denied, false);

        if (readable.Count > 0)
            return new(merge(readable), denied, true);

        var wasWithheld = supplied.Count > 0 || skipped.Concat(failed).Any(x => permissionOf(x) != null);

        return new(wasWithheld ? denied : merge(readable), denied, false);
    }

    /// <summary>A merged overview section, the <paramref name="Denied"/> form of it, and whether the caller read any of it.</summary>
    private sealed record OverviewSection<T>(T Value, T Denied, bool Readable);

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
