namespace Elsa.Mediator.Services;

public interface IMessageSender
{
    Task SendAsync<T>(T message, CancellationToken cancellationToken);
}