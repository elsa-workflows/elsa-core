using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;

namespace Elsa.Workflows.Admission;

// Owned work uses the synchronous exact-bookmark client lane in this first supported host.
// Ordinary queued requests may not compete with a held initial capability or manufacture lineage.
internal sealed class AdmissionWorkflowDispatcher(IWorkflowDispatcher inner, IWorkflowExecutionGuard guard) : IWorkflowDispatcher
{
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowDefinitionRequest request, DispatchWorkflowOptions? options = null, CancellationToken cancellationToken = default)
    {
        await DemandAsync(request.InstanceId, cancellationToken);
        return await inner.DispatchAsync(request, options, cancellationToken);
    }
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, DispatchWorkflowOptions? options = null, CancellationToken cancellationToken = default)
    {
        await DemandAsync(request.InstanceId, cancellationToken);
        return await inner.DispatchAsync(request, options, cancellationToken);
    }
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchTriggerWorkflowsRequest request, DispatchWorkflowOptions? options = null, CancellationToken cancellationToken = default)
    {
        await DemandAsync(request.WorkflowInstanceId, cancellationToken);
        return await inner.DispatchAsync(request, options, cancellationToken);
    }
    public async Task<DispatchWorkflowResponse> DispatchAsync(DispatchResumeWorkflowsRequest request, DispatchWorkflowOptions? options = null, CancellationToken cancellationToken = default)
    {
        await DemandAsync(request.WorkflowInstanceId, cancellationToken);
        return await inner.DispatchAsync(request, options, cancellationToken);
    }
    private ValueTask DemandAsync(string? instanceId, CancellationToken cancellationToken) => instanceId == null
        ? ValueTask.CompletedTask : guard.DemandUnownedAsync(instanceId, cancellationToken);
}
