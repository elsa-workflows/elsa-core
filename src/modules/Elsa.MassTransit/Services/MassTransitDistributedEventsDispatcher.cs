using Elsa.Workflows.Management.Handlers;
using Elsa.Workflows.Management.Requests;
using MassTransit;

namespace Elsa.MassTransit.Services;

/// <summary>
/// Dispatches workflow definition related notifications via MassTransit.
/// </summary>
public class MassTransitDistributedEventsDispatcher(IBus bus) : IDistributedEventsDispatcher
{
    /// <inheritdoc />
    public async Task DispatchAsync(RefreshWorkflowDefinitionsRequest request, CancellationToken cancellationToken = default)
    {
        await bus.Publish(request, cancellationToken);
    }
}