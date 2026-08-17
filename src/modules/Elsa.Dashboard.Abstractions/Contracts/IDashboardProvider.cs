using Elsa.Dashboard.Abstractions.Models;

namespace Elsa.Dashboard.Abstractions.Contracts;

public interface IDashboardProvider
{
    Task<DashboardOverview> GetOverviewAsync(DashboardQuery query, CancellationToken cancellationToken = default);

    Task<DashboardTrendResponse> GetWorkflowTrendsAsync(DashboardTrendRequest request, CancellationToken cancellationToken = default);

    Task<DashboardNeedsAttentionResponse> GetNeedsAttentionAsync(DashboardQuery query, int take, CancellationToken cancellationToken = default);

    Task<DashboardRecentActivityResponse> GetRecentActivityAsync(DashboardQuery query, int take, CancellationToken cancellationToken = default);

    Task<DashboardWorkflowHotspotsResponse> GetWorkflowHotspotsAsync(DashboardWorkflowHotspotsRequest request, CancellationToken cancellationToken = default);
}
