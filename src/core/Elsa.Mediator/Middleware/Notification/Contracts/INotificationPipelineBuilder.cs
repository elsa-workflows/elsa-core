namespace Elsa.Mediator.Middleware.Notification.Contracts;

public interface INotificationPipelineBuilder
{
    public IDictionary<string, object?> Properties { get; }
    IServiceProvider ApplicationServices { get; }
    INotificationPipelineBuilder Use(Func<NotificationMiddlewareDelegate, NotificationMiddlewareDelegate> middleware);
    public NotificationMiddlewareDelegate Build();
}