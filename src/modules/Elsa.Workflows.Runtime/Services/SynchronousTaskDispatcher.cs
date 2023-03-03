using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Notifications;
using IEventPublisher = Elsa.Mediator.Contracts.IEventPublisher;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Relies on the <see cref="IEventPublisher"/> to synchronously publish the received request as a domain event.
/// </summary>
public class SynchronousTaskDispatcher : ITaskDispatcher
{
    private readonly IEventPublisher _eventPublisher;

    /// <summary>
    /// Constructor.
    /// </summary>
    public SynchronousTaskDispatcher(IEventPublisher eventPublisher) => _eventPublisher = eventPublisher;

    /// <inheritdoc />
    public async Task DispatchAsync(RunTaskRequest request, CancellationToken cancellationToken = default) => await _eventPublisher.PublishAsync(request, cancellationToken);
}