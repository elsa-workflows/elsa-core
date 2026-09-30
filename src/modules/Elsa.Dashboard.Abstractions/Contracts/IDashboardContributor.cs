using Elsa.Dashboard.Abstractions.Models;

namespace Elsa.Dashboard.Abstractions.Contracts;

public interface IDashboardContributor
{
    string Id { get; }

    int Order { get; }

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

    /// <summary>The permissions guarding the sections this contribution supplies.</summary>
    public DashboardOverviewPermissions Permissions { get; init; } = new();
}

/// <summary>
/// The permission guarding each section of an overview contribution. A caller reads a section with the permission
/// declared here or with <c>dashboard:view</c>. A section supplied without a declaration needs <c>dashboard:view</c>, so
/// a contribution that declares nothing is never exposed to callers who hold only a narrower permission.
/// </summary>
public record DashboardOverviewPermissions
{
    public DashboardPermission? Runtime { get; init; }
    public DashboardPermission? WorkflowInstances { get; init; }
    public DashboardPermission? StructuredLogs { get; init; }
    public DashboardPermission? ConsoleLogs { get; init; }
}
