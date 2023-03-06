namespace Elsa.Mediator.Contracts;

public interface IConsumer<in T>
{
    ValueTask ConsumeAsync(T message, CancellationToken cancellationToken);
}