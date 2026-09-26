using System.Net;
using Elsa.Studio.Contracts;
using Elsa.Studio.Dashboard.Client;
using Elsa.Studio.Dashboard.Models;
using Refit;

namespace Elsa.Studio.Dashboard.Services;

public class DashboardService(IBackendApiClientProvider backendApiClientProvider) : IDashboardService
{
    public async Task<DashboardLoadResult<DashboardOverview>> LoadOverviewAsync(string range, bool includeSystem = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var api = await backendApiClientProvider.GetApiAsync<IDashboardApi>(cancellationToken);
            return DashboardLoadResult<DashboardOverview>.Loaded(await api.GetOverviewAsync(range, includeSystem, cancellationToken));
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return DashboardLoadResult<DashboardOverview>.Unavailable("Dashboard data is not available from this backend.");
        }
        catch (ApiException e) when (e.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return DashboardLoadResult<DashboardOverview>.Unauthorized("You do not have access to dashboard data for this backend.");
        }
        catch (HttpRequestException e)
        {
            return DashboardLoadResult<DashboardOverview>.BackendDisconnected(e.Message);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return DashboardLoadResult<DashboardOverview>.BackendDisconnected("The dashboard request timed out.");
        }
    }

    public async Task<DashboardLoadResult> LoadAsync(string range, bool includeSystem = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var api = await backendApiClientProvider.GetApiAsync<IDashboardApi>(cancellationToken);
            var overviewTask = api.GetOverviewAsync(range, includeSystem, cancellationToken);
            var needsAttentionTask = api.GetNeedsAttentionAsync(range, 8, includeSystem, cancellationToken);
            var trendsTask = api.GetWorkflowTrendsAsync(new DashboardTrendRequest
            {
                Range = range,
                Granularity = DashboardRangeMapper.GetDefaultGranularity(range),
                IncludeSystem = includeSystem
            }, cancellationToken);
            var recentActivityTask = api.GetRecentActivityAsync(range, 20, includeSystem, cancellationToken);
            var hotspotsTask = TryGetHotspotsAsync(api, range, includeSystem, cancellationToken);

            await Task.WhenAll(overviewTask, needsAttentionTask, trendsTask, recentActivityTask, hotspotsTask);

            return DashboardLoadResult.Loaded(new DashboardSnapshot(
                await overviewTask,
                await needsAttentionTask,
                await trendsTask,
                await recentActivityTask,
                await hotspotsTask));
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return DashboardLoadResult.Unavailable("Dashboard data is not available from this backend.");
        }
        catch (ApiException e) when (e.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return DashboardLoadResult.Unauthorized("You do not have access to dashboard data for this backend.");
        }
        catch (HttpRequestException e)
        {
            return DashboardLoadResult.BackendDisconnected(e.Message);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return DashboardLoadResult.BackendDisconnected("The dashboard request timed out.");
        }
    }

    private static async Task<DashboardWorkflowHotspotsResponse?> TryGetHotspotsAsync(IDashboardApi api, string range, bool includeSystem, CancellationToken cancellationToken)
    {
        try
        {
            return await api.GetWorkflowHotspotsAsync(new DashboardWorkflowHotspotsRequest
            {
                Range = range,
                Metric = DashboardHotspotMetric.Faults,
                Take = 8,
                IncludeSystem = includeSystem
            }, cancellationToken);
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
