using Elsa.MassTransit.Contracts;
using Elsa.MassTransit.Messages;
using MassTransit;

namespace Elsa.MassTransit.Services;

/// <summary>
/// Dispatches workflow definition related notifications via MassTransit.
/// </summary>
public class MassTransitDistributedEventsDispatcher(IBus bus) : IDistributedWorkflowDefinitionEventsDispatcher
{
    /// <inheritdoc />
    public Task DispatchAsync(WorkflowDefinitionPublished request, CancellationToken cancellationToken)
    {
        return bus.Publish(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task DispatchAsync(WorkflowDefinitionRetracted request, CancellationToken cancellationToken)
    {
        return bus.Publish(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task DispatchAsync(WorkflowDefinitionDeleted request, CancellationToken cancellationToken)
    {
        return bus.Publish(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task DispatchAsync(WorkflowDefinitionsDeleted request, CancellationToken cancellationToken)
    {
        return bus.Publish(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task DispatchAsync(WorkflowDefinitionVersionDeleted request, CancellationToken cancellationToken)
    {
        return bus.Publish(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task DispatchAsync(WorkflowDefinitionVersionsDeleted request, CancellationToken cancellationToken)
    {
        return bus.Publish(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task DispatchAsync(WorkflowDefinitionVersionsUpdated request, CancellationToken cancellationToken)
    {
        return bus.Publish(request, cancellationToken);
    }
}