using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Relies on the <see cref="INotificationSender"/> to publish the received request as a domain event from a background worker.
/// </summary>
public class BackgroundTaskDispatcher : ITaskDispatcher
{
    private readonly INotificationSender _eventPublisher;

    /// <summary>
    /// Constructor.
    /// </summary>
    public BackgroundTaskDispatcher(INotificationSender eventPublisher) => _eventPublisher = eventPublisher;

    /// <inheritdoc />
    public async Task DispatchAsync(RunTaskRequest request, CancellationToken cancellationToken = default) => await _eventPublisher.SendAsync(request, NotificationStrategy.Background, cancellationToken);
}