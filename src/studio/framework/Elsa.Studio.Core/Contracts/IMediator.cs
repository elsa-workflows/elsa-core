namespace Elsa.Studio.Contracts;

/// <summary>
/// Represents a mediator that can be used to publish notifications and subscribe to notifications.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Subscribes to a notification.
    /// </summary>
    /// <param name="handler"></param>
    /// <typeparam name="TNotification"></typeparam>
    /// <typeparam name="THandler"></typeparam>
    void Subscribe<TNotification, THandler>(THandler handler) where THandler : INotificationHandler<TNotification> where TNotification : INotification;
    
    /// <summary>
    /// Unsubscribes from a notification.
    /// </summary>
    void Unsubscribe<TNotification, THandler>(THandler handler) where THandler : INotificationHandler<TNotification> where TNotification : INotification;
    
    /// <summary>
    /// Unsubscribes from a notification.
    /// </summary>
    void Unsubscribe(INotificationHandler handler);
    
    /// <summary>
    /// Publishes a notification.
    /// </summary>
    Task NotifyAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification;
}