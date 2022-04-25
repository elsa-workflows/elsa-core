namespace Elsa.Mediator.Services;

public interface IEventPublisher
{
    Task PublishAsync(INotification notification, CancellationToken cancellationToken = default);
}