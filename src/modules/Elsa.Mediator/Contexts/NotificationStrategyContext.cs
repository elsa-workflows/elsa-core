using Elsa.Mediator.Contracts;
using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.Contexts;

/// <summary>
/// Represents a context for publishing events.
/// </summary>
/// <param name="Notification">The notification to publish.</param>
/// <param name="Handlers">The handlers to publish the notification to.</param>
/// <param name="Logger">The logger.</param>
/// <param name="ServiceProvider">The service provider to resolve services from.</param>
/// <param name="CancellationToken">The cancellation token.</param>
public record NotificationStrategyContext(INotification Notification, INotificationHandler[] Handlers, ILogger Logger, IServiceProvider ServiceProvider, CancellationToken CancellationToken = default);