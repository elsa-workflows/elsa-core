namespace Elsa.Mediator.Middleware.Notification.Contracts;

public interface INotificationMiddleware
{
    ValueTask InvokeAsync(NotificationContext context);
}