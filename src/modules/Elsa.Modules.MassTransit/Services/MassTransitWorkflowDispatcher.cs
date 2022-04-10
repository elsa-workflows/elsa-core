using Elsa.Runtime.Contracts;
using Elsa.Runtime.Models;
using MassTransit;

namespace Elsa.Modules.MassTransit.Services;

public class MassTransitWorkflowDispatcher : IWorkflowDispatcher
{
    private readonly IBus _bus;

    public MassTransitWorkflowDispatcher(IBus bus)
    {
        _bus = bus;
    }

    public async Task<DispatchWorkflowDefinitionResponse> DispatchAsync(DispatchWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        await _bus.Send(request, cancellationToken);
        return new();
    }

    public async Task<DispatchWorkflowInstanceResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        await _bus.Send(request, cancellationToken);
        return new();
    }
}