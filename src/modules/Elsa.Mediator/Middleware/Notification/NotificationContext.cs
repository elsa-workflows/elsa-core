using Elsa.Mediator.Contracts;
using Elsa.Mediator.PublishingStrategies;

namespace Elsa.Mediator.Middleware.Notification;

/// <summary>
/// Represents a context for a notification.
/// </summary>
public class NotificationContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationContext"/> class.
    /// </summary>
    /// <param name="notification"></param>
    /// <param name="publishingStrategy">Default value is <see cref="FireAndForgetStrategy"/></param>
    /// <param name="cancellationToken"></param>
    public NotificationContext(INotification notification, IEventPublishingStrategy? publishingStrategy, CancellationToken cancellationToken = default)
    {
        Notification = notification;
        PublishingStrategy = publishingStrategy ?? new FireAndForgetStrategy();
        CancellationToken = cancellationToken;
    }

    public INotification Notification { get; init; }
    public IEventPublishingStrategy PublishingStrategy { get; init; }
    public CancellationToken CancellationToken { get; init; }
}