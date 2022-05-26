namespace Elsa.Mediator.Middleware.Notification.Contracts;

public interface INotificationPipeline
{
    NotificationMiddlewareDelegate Setup(Action<INotificationPipelineBuilder> setup);
    NotificationMiddlewareDelegate Pipeline { get; }
    Task ExecuteAsync(NotificationContext context);
}