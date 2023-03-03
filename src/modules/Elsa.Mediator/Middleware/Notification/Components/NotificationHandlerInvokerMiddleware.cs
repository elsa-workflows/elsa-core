using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Notification.Contracts;

namespace Elsa.Mediator.Middleware.Notification.Components;

public class NotificationHandlerInvokerMiddleware : INotificationMiddleware
{
    private readonly NotificationMiddlewareDelegate _next;
    private readonly IEnumerable<INotificationHandler> _notificationHandlers;

    public NotificationHandlerInvokerMiddleware(NotificationMiddlewareDelegate next, IEnumerable<INotificationHandler> notificationHandlers)
    {
        _next = next;
        _notificationHandlers = notificationHandlers;
    }

    public async ValueTask InvokeAsync(NotificationContext context)
    {
        // Find all handlers for the specified notification.
        var notification = context.Notification;
        var notificationType = notification.GetType();
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
        var handlers = _notificationHandlers.Where(x => handlerType.IsInstanceOfType(x)).ToArray();
        var handleMethod = handlerType.GetMethod("HandleAsync")!;
        var cancellationToken = context.CancellationToken;

        foreach (var handler in handlers)
        {
            var task = (Task)handleMethod.Invoke(handler, new object?[] { notification, cancellationToken })!;
            await task;
        }

        // Invoke next middleware.
        await _next(context);
    }
}