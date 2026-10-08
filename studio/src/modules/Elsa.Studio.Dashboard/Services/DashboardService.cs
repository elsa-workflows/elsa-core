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

            // The instance endpoints may refuse a caller the overview accepts (such as one whose permissions are unknown to the
            // host, or a backend that serves the overview more widely than them): the overview stays, the refused parts are absent.
            var needsAttentionTask = WhenRefusedAsync(api.GetNeedsAttentionAsync(range, 8, includeSystem, cancellationToken));
            var trendsTask = WhenRefusedAsync(api.GetWorkflowTrendsAsync(new DashboardTrendRequest
            {
                Range = range,
                Granularity = DashboardRangeMapper.GetDefaultGranularity(range),
                IncludeSystem = includeSystem
            }, cancellationToken));
            var recentActivityTask = WhenRefusedAsync(api.GetRecentActivityAsync(range, 20, includeSystem, cancellationToken));
            var hotspotsTask = TryGetHotspotsAsync(api, range, includeSystem, cancellationToken);

            await Task.WhenAll(overviewTask, needsAttentionTask, trendsTask, recentActivityTask, hotspotsTask);

            return DashboardLoadResult.Loaded(new DashboardSnapshot(
                await overviewTask,
                await needsAttentionTask ?? new DashboardNeedsAttentionResponse { Capability = DashboardCapabilityStatus.Unauthorized },
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

    private static async Task<T?> WhenRefusedAsync<T>(Task<T> request)
    {
        try
        {
            return await request;
        }
        catch (ApiException e) when (e.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return default;
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
        catch (ApiException e) when (e.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return null;
        }
    }
}
