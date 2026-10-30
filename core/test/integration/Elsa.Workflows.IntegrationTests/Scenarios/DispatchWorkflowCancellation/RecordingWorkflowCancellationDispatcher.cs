using System.Collections.Concurrent;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DispatchWorkflowCancellation;

/// <summary>
/// Records the ID of every workflow instance whose cancellation is dispatched, then delegates. The cancellation service
/// awaits its dispatches, so once a call into it returns, every instance it decided to cancel has been recorded, even
/// though the cancellations themselves still run in the background.
/// </summary>
public class RecordingWorkflowCancellationDispatcher(IWorkflowCancellationDispatcher inner, ConcurrentQueue<string> targets) : IWorkflowCancellationDispatcher
{
    public Task<DispatchCancelWorkflowsResponse> DispatchAsync(DispatchCancelWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        targets.Enqueue(request.WorkflowInstanceId);
        return inner.DispatchAsync(request, cancellationToken);
    }
}
