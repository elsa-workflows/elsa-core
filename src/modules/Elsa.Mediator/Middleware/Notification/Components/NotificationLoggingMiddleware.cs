using Elsa.Mediator.Middleware.Notification.Contracts;

namespace Elsa.Mediator.Middleware.Notification.Components;

public class NotificationLoggingMiddleware : INotificationMiddleware
{
    private readonly NotificationMiddlewareDelegate _next;
    public NotificationLoggingMiddleware(NotificationMiddlewareDelegate next) => _next = next;

    public async ValueTask InvokeAsync(NotificationContext context)
    {
        // Invoke next middleware.
        await _next(context);
    }
}