using Elsa.Dashboard.Abstractions.Models;

namespace Elsa.Dashboard.Abstractions.Contracts;

public interface IDashboardContributor
{
    string Id { get; }

    int Order { get; }

    /// <summary>
    /// Optional, up front: the permissions of everything this contributor adds to the overview (its sections, metric
    /// cards and panels); used only to skip the overview call for a caller who can read none of them. A contributor
    /// must therefore declare every permission its cards and panels carry: one that does not is skipped for a caller who
    /// holds only the undeclared permission. An undeclared section still needs <c>dashboard:view</c>.
    /// The other calls (findings, trends, recent activity and hotspots) always invoke the contributor and filter by the
    /// permission on what it returns. When <c>null</c> the contributor always runs.
    /// </summary>
    DashboardOverviewPermissions? OverviewPermissions => null;

    ValueTask<DashboardOverviewContribution?> GetOverviewAsync(DashboardContext context)
    {
        return ValueTask.FromResult<DashboardOverviewContribution?>(null);
    }

    ValueTask<IReadOnlyCollection<DashboardFinding>> GetFindingsAsync(DashboardContext context)
    {
        return ValueTask.FromResult<IReadOnlyCollection<DashboardFinding>>([]);
    }

    ValueTask<DashboardTrendResponse?> GetWorkflowTrendsAsync(DashboardTrendContext context)
    {
        return ValueTask.FromResult<DashboardTrendResponse?>(null);
    }

    ValueTask<DashboardRecentActivityResponse?> GetRecentActivityAsync(DashboardListContext context)
    {
        return ValueTask.FromResult<DashboardRecentActivityResponse?>(null);
    }

    ValueTask<DashboardWorkflowHotspotsResponse?> GetWorkflowHotspotsAsync(DashboardHotspotsContext context)
    {
        return ValueTask.FromResult<DashboardWorkflowHotspotsResponse?>(null);
    }
}

public record DashboardContext(
    DashboardRange Range,
    bool IncludeSystem,
    CancellationToken CancellationToken,
    string? TenantId = null,
    string? EnvironmentName = null)
{
    /// <summary>
    /// Whether the caller may read data guarded by a permission, with the dashboard's default rule already applied
    /// (<c>dashboard:view</c> reads everything, and data with no declared permission needs it). It only tells a
    /// contributor which queries it can skip; the provider still filters what the contributor returns.
    /// <c>null</c> means unrestricted.
    /// </summary>
    public Func<DashboardPermission?, bool>? CanRead { get; init; }
}

public record DashboardTrendContext(
    DashboardRange Range,
    string Granularity,
    bool IncludeSystem,
    CancellationToken CancellationToken,
    string? TenantId = null,
    string? EnvironmentName = null);

public record DashboardListContext(
    DashboardRange Range,
    int Take,
    bool IncludeSystem,
    CancellationToken CancellationToken,
    string? TenantId = null,
    string? EnvironmentName = null);

public record DashboardHotspotsContext(
    DashboardRange Range,
    string Metric,
    int Take,
    bool IncludeSystem,
    CancellationToken CancellationToken,
    string? TenantId = null,
    string? EnvironmentName = null);

public record DashboardOverviewContribution
{
    public DashboardRuntimeStatus? Runtime { get; init; }
    public DashboardWorkflowInstanceMetrics? WorkflowInstances { get; init; }
    public DashboardDiagnosticsSummary? Diagnostics { get; init; }
    public IReadOnlyCollection<DashboardMetricCard> Metrics { get; init; } = [];
    public IReadOnlyCollection<DashboardPanelSummary> Panels { get; init; } = [];
}

/// <summary>
/// The permission guarding each overview section a contributor supplies. A caller reads a section with the permission
/// declared here or with <c>dashboard:view</c>. A section supplied without a declaration needs <c>dashboard:view</c>, so
/// a contributor that declares nothing is never exposed to callers who hold only a narrower permission.
/// </summary>
public record DashboardOverviewPermissions
{
    public DashboardPermission? Runtime { get; init; }
    public DashboardPermission? WorkflowInstances { get; init; }
    public DashboardPermission? StructuredLogs { get; init; }
    public DashboardPermission? ConsoleLogs { get; init; }

    /// <summary>The permissions declared for any section.</summary>
    public IEnumerable<DashboardPermission> All() => new[] { Runtime, WorkflowInstances, StructuredLogs, ConsoleLogs }.OfType<DashboardPermission>();
}
