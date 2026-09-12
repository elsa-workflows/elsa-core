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
}
