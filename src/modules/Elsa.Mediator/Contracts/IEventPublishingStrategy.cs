using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.Contracts;

/// <summary>
/// Represents a strategy for publishing events.
/// </summary>
public interface IEventPublishingStrategy
{
    Task PublishAsync(INotification notification, INotificationHandler[] handlers, ILogger logger, CancellationToken cancellationToken = default);
}