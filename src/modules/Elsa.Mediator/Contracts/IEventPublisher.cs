namespace Elsa.Mediator.Contracts;

/// <summary>
/// Publishes notifications.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes the given notification.
    /// </summary>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PublishAsync(INotification notification, CancellationToken cancellationToken = default);
}