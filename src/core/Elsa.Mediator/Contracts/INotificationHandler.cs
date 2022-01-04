namespace Elsa.Mediator.Contracts;

public interface INotificationHandler
{
}

public interface INotificationHandler<in T> : INotificationHandler where T : INotification
{
    Task HandleAsync(T notification, CancellationToken cancellationToken);
}