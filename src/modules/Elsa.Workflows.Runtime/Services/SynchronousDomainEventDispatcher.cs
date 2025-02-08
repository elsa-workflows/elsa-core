using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Relies on the <see cref="INotificationSender"/> to synchronously publish the received request as a domain event.
/// </summary>
public class SynchronousDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly INotificationSender _notificationSender;

    /// <summary>
    /// Constructor.
    /// </summary>
    public SynchronousDomainEventDispatcher(INotificationSender notificationSender) => _notificationSender = notificationSender;

    /// <inheritdoc />
    public async Task DispatchAsync(DomainEventNotification request, CancellationToken cancellationToken = default) => await _notificationSender.SendAsync(request, cancellationToken);
}