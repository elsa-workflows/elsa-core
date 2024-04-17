using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;
using MassTransit;

namespace Elsa.MassTransit.Services;

/// <summary>
/// Dispatches workflow cancellation requests to a MassTransit bus.
/// </summary>
public class MassTransitWorkflowCancellationDispatcher(IBus bus) : IWorkflowCancellationDispatcher
{
    /// <inheritdoc />
    public async Task<DispatchCancelWorkflowsResponse> DispatchAsync(DispatchCancelWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        await bus.Publish(request, cancellationToken);
        return new();
    }
}