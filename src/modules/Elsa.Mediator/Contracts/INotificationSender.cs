using Elsa.Mediator.PublishingStrategies;

namespace Elsa.Mediator.Contracts;

/// <summary>
/// Publishes notifications.
/// </summary>
public interface INotificationSender
{
    /// <summary>
    /// Publishes the given notification.
    /// </summary>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="strategy"><see cref="FireAndForgetStrategy"/><see cref="SequentialProcessingStrategy"/><see cref="ParallelProcessingStrategy"/></param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SendAsync(INotification notification, IEventPublishingStrategy? strategy = default, CancellationToken cancellationToken = default);
}