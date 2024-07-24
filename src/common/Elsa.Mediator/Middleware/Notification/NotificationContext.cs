using Elsa.Mediator.Contracts;

namespace Elsa.Mediator.Middleware.Notification;

/// <summary>
/// Represents a context for a notification.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NotificationContext"/> class.
/// </remarks>
/// <param name="notification">The notification to publish.</param>
/// <param name="notificationStrategy">The publishing strategy to use.</param>
/// <param name="cancellationToken">The cancellation token.</param>
public class NotificationContext(INotification notification, 
    IEventPublishingStrategy notificationStrategy,
    CancellationToken cancellationToken = default)
{

    /// <summary>
    /// Gets the notification to publish.
    /// </summary>
    public INotification Notification { get; init; } = notification;

    /// <summary>
    /// Gets the publishing strategy to use.
    /// </summary>
    public IEventPublishingStrategy NotificationStrategy { get; init; } = notificationStrategy;

    /// <summary>
    /// Gets the cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = cancellationToken;
}