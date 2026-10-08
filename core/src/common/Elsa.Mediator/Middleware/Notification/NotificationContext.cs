using Elsa.Mediator.Contracts;

namespace Elsa.Mediator.Middleware.Notification;

/// <summary>
/// Represents a context for a notification.
/// </summary>
public class NotificationContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationContext"/> class.
    /// </summary>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="notificationStrategy">The publishing strategy to use.</param>
    /// <param name="serviceProvider">The service provider to resolve services from.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public NotificationContext(INotification notification, IEventPublishingStrategy notificationStrategy, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        Notification = notification;
        NotificationStrategy = notificationStrategy;
        ServiceProvider = serviceProvider;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the notification to publish.
    /// </summary>
    public INotification Notification { get; init; }
    
    /// <summary>
    /// Gets the publishing strategy to use.
    /// </summary>
    public IEventPublishingStrategy NotificationStrategy { get; init; }

    /// <summary>
    /// Gets the service provider used for resolving dependencies within the notification context.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets the cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }
}