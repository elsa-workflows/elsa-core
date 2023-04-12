using Elsa.Mediator.Middleware.Notification.Contracts;

namespace Elsa.Mediator.Middleware.Notification.Components;

/// <summary>
/// A notification middleware that logs the notification.
/// </summary>
public class NotificationLoggingMiddleware : INotificationMiddleware
{
    private readonly NotificationMiddlewareDelegate _next;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationLoggingMiddleware"/> class.
    /// </summary>
    public NotificationLoggingMiddleware(NotificationMiddlewareDelegate next) => _next = next;

    /// <inheritdoc />
    public async ValueTask InvokeAsync(NotificationContext context)
    {
        // TODO: Log notification.
        
        // Invoke next middleware.
        await _next(context);
    }
}