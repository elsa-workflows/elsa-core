using System.Collections.Concurrent;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DispatchWorkflowCancellation;

/// <summary>
/// Records the instance ID of every workflow definition dispatch it delivers, then delegates. Used as the inner dispatcher
/// the transactional outbox delivers through, so once the outbox processor returns, every child it handed over for
/// execution has been recorded, even though the children themselves would start in the background.
/// </summary>
public class RecordingWorkflowDispatcher(IWorkflowDispatcher inner, ConcurrentQueue<string> deliveredInstanceIds) : IWorkflowDispatcher
{
    public Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowDefinitionRequest request, DispatchWorkflowOptions? options = null, CancellationToken cancellationToken = default)
    {
        deliveredInstanceIds.Enqueue(request.InstanceId!);
        return inner.DispatchAsync(request, options, cancellationToken);
    }

    public Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, DispatchWorkflowOptions? options = null, CancellationToken cancellationToken = default) =>
        inner.DispatchAsync(request, options, cancellationToken);

    public Task<DispatchWorkflowResponse> DispatchAsync(DispatchTriggerWorkflowsRequest request, DispatchWorkflowOptions? options = null, CancellationToken cancellationToken = default) =>
        inner.DispatchAsync(request, options, cancellationToken);

    public Task<DispatchWorkflowResponse> DispatchAsync(DispatchResumeWorkflowsRequest request, DispatchWorkflowOptions? options = null, CancellationToken cancellationToken = default) =>
        inner.DispatchAsync(request, options, cancellationToken);
}
