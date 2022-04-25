namespace Elsa.Mediator.Services;

public interface INotificationHandler
{
}

public interface INotificationHandler<in T> : INotificationHandler where T : INotification
{
    Task HandleAsync(T notification, CancellationToken cancellationToken);
}