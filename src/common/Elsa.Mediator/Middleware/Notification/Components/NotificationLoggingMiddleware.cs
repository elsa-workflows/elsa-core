using Elsa.Mediator.Middleware.Notification.Contracts;

namespace Elsa.Mediator.Middleware.Notification.Components;

/// <summary>
/// A notification middleware that logs the notification.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NotificationLoggingMiddleware"/> class.
/// </remarks>
public class NotificationLoggingMiddleware(NotificationMiddlewareDelegate next) : INotificationMiddleware
{
    private readonly NotificationMiddlewareDelegate _next = next;

    /// <inheritdoc />
    public async ValueTask InvokeAsync(NotificationContext context) =>
        // TODO: Log notification.

        // Invoke next middleware.
        await _next(context);
}