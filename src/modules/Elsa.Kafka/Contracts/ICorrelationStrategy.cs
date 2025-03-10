namespace Elsa.Kafka;

/// <summary>
/// Interface representing a strategy for extracting a correlation ID from a <see cref="KafkaTransportMessage"/>.
/// </summary>
public interface ICorrelationStrategy
{
    string? GetCorrelationId(KafkaTransportMessage transportMessage);
}