using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityExecutions.Contracts;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Requests;
using Elsa.Api.Client.Resources.Resilience.Contracts;
using Elsa.Api.Client.Resources.Resilience.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// An activity execution service that uses a remote backend to retrieve activity execution reports.
/// </summary>
public class RemoteActivityExecutionService(IBackendApiClientProvider backendApiClientProvider, IRemoteFeatureProvider remoteFeatureProvider) : IActivityExecutionService
{
    private bool? _isResilienceEnabled;

    /// <inheritdoc />
    public async Task<ActivityExecutionReport> GetReportAsync(string workflowInstanceId, JsonObject containerActivity, CancellationToken cancellationToken = default)
    {
        var activityNodeIds = containerActivity.GetActivities().Select(x => x.GetNodeId()).ToList();
        var request = new GetActivityExecutionReportRequest
        {
            WorkflowInstanceId = workflowInstanceId,
            ActivityNodeIds = activityNodeIds
        };
        var api = await backendApiClientProvider.GetApiAsync<IActivityExecutionsApi>(cancellationToken);
        return await api.GetReportAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecord>> ListAsync(string workflowInstanceId, string activityNodeId, CancellationToken cancellationToken = default)
    {
        var request = new ListActivityExecutionsRequest
        {
            WorkflowInstanceId = workflowInstanceId,
            ActivityNodeId = activityNodeId
        };

        var api = await backendApiClientProvider.GetApiAsync<IActivityExecutionsApi>(cancellationToken);
        var response = await api.ListAsync(request, cancellationToken);
        return response.Items;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecordSummary>> ListSummariesAsync(string workflowInstanceId, string activityNodeId, CancellationToken cancellationToken = default)
    {
        var request = new ListActivityExecutionsRequest
        {
            WorkflowInstanceId = workflowInstanceId,
            ActivityNodeId = activityNodeId
        };

        var api = await backendApiClientProvider.GetApiAsync<IActivityExecutionsApi>(cancellationToken);
        var response = await api.ListSummariesAsync(request, cancellationToken);
        return response.Items;
    }

    /// <inheritdoc />
    public async Task<ActivityExecutionRecord?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<IActivityExecutionsApi>(cancellationToken);
        return await api.GetAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ActivityExecutionCallStack> GetCallStackAsync(string activityExecutionId, bool? includeCrossWorkflowChain = null, int? skip = null, int? take = null, CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<IActivityExecutionsApi>(cancellationToken);
        return await api.GetCallStackAsync(activityExecutionId, includeCrossWorkflowChain, skip, take, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PagedListResponse<RetryAttemptRecord>> GetRetriesAsync(string activityInstanceId, int? skip = null, int? take = null, CancellationToken cancellationToken = default)
    {
        // Check if the Resilience feature is enabled before making API calls.
        // Cache the result for the lifetime of this scoped service to avoid repeated backend requests.
        _isResilienceEnabled ??= await remoteFeatureProvider.IsEnabledAsync("Elsa.Resilience", cancellationToken);

        if (!_isResilienceEnabled.Value)
            return new() { Items = [] };

        var api = await backendApiClientProvider.GetApiAsync<IRetryAttemptsApi>(cancellationToken);
        return await api.ListAsync(activityInstanceId, skip, take, cancellationToken);
    }
}
