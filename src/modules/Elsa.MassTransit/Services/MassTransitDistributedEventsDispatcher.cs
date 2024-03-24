using Elsa.MassTransit.Contracts;
using Elsa.MassTransit.Messages;
using MassTransit;

namespace Elsa.MassTransit.Services;

/// <summary>
/// Dispatches workflow definition related notifications via MassTransit.
/// </summary>
public class MassTransitDistributedEventsDispatcher(IBus bus) : IDistributedWorkflowDefinitionEventsDispatcher
{
    public Task DispatchAsync(WorkflowDefinitionPublished request, CancellationToken cancellationToken)
    {
        return bus.Publish(request, cancellationToken);
    }

    public Task DispatchAsync(WorkflowDefinitionRetracted request, CancellationToken cancellationToken)
    {
        return bus.Publish(request, cancellationToken);
    }

    public Task DispatchAsync(WorkflowDefinitionDeleted request, CancellationToken cancellationToken)
    {
        return bus.Publish(request, cancellationToken);
    }

    public Task DispatchAsync(WorkflowDefinitionsDeleted request, CancellationToken cancellationToken)
    {
        return bus.Publish(request, cancellationToken);
    }

    public Task DispatchAsync(WorkflowDefinitionCreated request, CancellationToken cancellationToken)
    {
        return bus.Publish(request, cancellationToken);
    }

    public Task DispatchAsync(WorkflowDefinitionVersionDeleted request, CancellationToken cancellationToken)
    {
        return bus.Publish(request, cancellationToken);
    }

    public Task DispatchAsync(WorkflowDefinitionVersionsDeleted request, CancellationToken cancellationToken)
    {
        return bus.Publish(request, cancellationToken);
    }
}