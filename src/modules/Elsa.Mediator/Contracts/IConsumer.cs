namespace Elsa.Mediator.Contracts;

/// <summary>
/// Represents a consumer of a given message type.
/// </summary>
/// <typeparam name="T">The type of the message to consume.</typeparam>
public interface IConsumer<in T>
{
    /// <summary>
    /// Consumes the given message.
    /// </summary>
    /// <param name="message">The message to consume.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask ConsumeAsync(T message, CancellationToken cancellationToken);
}