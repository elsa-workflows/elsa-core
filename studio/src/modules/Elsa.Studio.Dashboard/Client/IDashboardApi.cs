using Elsa.Studio.Dashboard.Models;
using Refit;

namespace Elsa.Studio.Dashboard.Client;

/// <summary>
/// Backend API for the operational dashboard.
/// </summary>
public interface IDashboardApi
{
    [Get("/dashboard/overview")]
    Task<DashboardOverview> GetOverviewAsync(
        [AliasAs("range")] string? range = null,
        [AliasAs("includeSystem")] bool includeSystem = false,
        CancellationToken cancellationToken = default);

    [Post("/dashboard/workflow-trends")]
    Task<DashboardTrendResponse> GetWorkflowTrendsAsync(
        [Body] DashboardTrendRequest request,
        CancellationToken cancellationToken = default);

    [Get("/dashboard/needs-attention")]
    Task<DashboardNeedsAttentionResponse> GetNeedsAttentionAsync(
        [AliasAs("range")] string? range = null,
        [AliasAs("take")] int take = 8,
        [AliasAs("includeSystem")] bool includeSystem = false,
        CancellationToken cancellationToken = default);

    [Get("/dashboard/recent-activity")]
    Task<DashboardRecentActivityResponse> GetRecentActivityAsync(
        [AliasAs("range")] string? range = null,
        [AliasAs("take")] int take = 20,
        [AliasAs("includeSystem")] bool includeSystem = false,
        CancellationToken cancellationToken = default);

    [Post("/dashboard/workflow-hotspots")]
    Task<DashboardWorkflowHotspotsResponse> GetWorkflowHotspotsAsync(
        [Body] DashboardWorkflowHotspotsRequest request,
        CancellationToken cancellationToken = default);
}
