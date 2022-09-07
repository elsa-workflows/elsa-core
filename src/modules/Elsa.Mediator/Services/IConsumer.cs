namespace Elsa.Mediator.Services;

public interface IConsumer<in T>
{
    ValueTask ConsumeAsync(T message, CancellationToken cancellationToken);
}