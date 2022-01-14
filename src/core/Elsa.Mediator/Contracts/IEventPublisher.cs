namespace Elsa.Mediator.Contracts;

public interface IEventPublisher
{
    Task PublishAsync(INotification notification, CancellationToken cancellationToken = default);
}