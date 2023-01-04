using Elsa.Mediator.Services;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Implementations;

/// <summary>
/// Relies on the <see cref="IBackgroundEventPublisher"/> to asynchronously publish the received request as a domain event.
/// </summary>
public class AsynchronousTaskDispatcher : ITaskDispatcher
{
    private readonly IBackgroundEventPublisher _eventPublisher;

    /// <summary>
    /// Constructor.
    /// </summary>
    public AsynchronousTaskDispatcher(IBackgroundEventPublisher eventPublisher) => _eventPublisher = eventPublisher;

    /// <inheritdoc />
    public async Task DispatchAsync(RunTaskRequest request, CancellationToken cancellationToken = default) => await _eventPublisher.PublishAsync(request, cancellationToken);
}