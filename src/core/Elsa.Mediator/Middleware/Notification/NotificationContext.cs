using Elsa.Mediator.Contracts;

namespace Elsa.Mediator.Middleware.Notification;

public class NotificationContext
{
    public NotificationContext(INotification notification, CancellationToken cancellationToken)
    {
        Notification = notification;
        CancellationToken = cancellationToken;
    }

    public INotification Notification { get; init; }
    public CancellationToken CancellationToken { get; init; }
}