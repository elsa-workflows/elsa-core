namespace Elsa.ServiceBus.Abstractions.Services;

public interface IServiceBus
{
    Task SendAsync(object message, CancellationToken cancellationToken = default);
    Task PublishAsync(object message, CancellationToken cancellationToken = default);
}