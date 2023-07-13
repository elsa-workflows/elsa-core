using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.Contracts;

/// <summary>
/// Represents a context for publishing events.
/// </summary>
/// <param name="Notification">The notification to publish.</param>
/// <param name="Handlers">The handlers to publish the notification to.</param>
/// <param name="Logger">The logger.</param>
/// <param name="CancellationToken">The cancellation token.</param>
public record PublishContext(INotification Notification, INotificationHandler[] Handlers, ILogger Logger, CancellationToken CancellationToken = default);