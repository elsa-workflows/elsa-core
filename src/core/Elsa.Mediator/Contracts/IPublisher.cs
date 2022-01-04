namespace Elsa.Mediator.Contracts;

public interface IPublisher
{
    Task PublishAsync(INotification notification, CancellationToken cancellationToken = default);
}