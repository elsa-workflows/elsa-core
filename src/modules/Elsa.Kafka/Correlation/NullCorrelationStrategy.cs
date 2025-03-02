namespace Elsa.Kafka;

/// <summary>
/// Represents a strategy that does not extract any correlation ID from a <see cref="KafkaTransportMessage"/>.
/// This implementation of <see cref="ICorrelationStrategy"/> always returns null,
/// effectively indicating no correlation ID is available or applicable.
/// </summary>
public class NullCorrelationStrategy : ICorrelationStrategy
{
    public string? GetCorrelationId(KafkaTransportMessage transportMessage) => null;
}