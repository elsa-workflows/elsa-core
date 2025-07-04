using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Relies on the <see cref="INotificationSender"/> to publish the received request as a domain event from a background worker.
/// </summary>
public class BackgroundDomainEventDispatcher(INotificationSender notificationSender) : IDomainEventDispatcher
{
    /// <inheritdoc />
    public async Task DispatchAsync(DomainEventNotification request, CancellationToken cancellationToken = default)
    {
        await notificationSender.SendAsync(request, NotificationStrategy.Background, cancellationToken);
    }
}
