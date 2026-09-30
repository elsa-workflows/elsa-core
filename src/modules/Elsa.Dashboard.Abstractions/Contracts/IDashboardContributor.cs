using Elsa.Dashboard.Abstractions.Models;

namespace Elsa.Dashboard.Abstractions.Contracts;

public interface IDashboardContributor
{
    string Id { get; }

    int Order { get; }

    /// <summary>
    /// Optional, up front: the permission guarding each overview section this contributor supplies. A caller reads a
    /// section with the permission declared here or with <c>dashboard:view</c>. The dashboard does not invoke a
    /// contributor that declares permissions when the caller may read none of them, and marks the sections it
    /// declared as unauthorized. Declare every permission used by anything the contributor supplies: its findings, trends,
    /// recent activity and hotspots as well as the overview. When <c>null</c> the contributor always runs, and whatever
    /// it supplies without a permission needs <c>dashboard:view</c>.
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
    string? EnvironmentName = null);

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
