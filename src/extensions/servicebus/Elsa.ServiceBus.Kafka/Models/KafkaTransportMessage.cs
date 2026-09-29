namespace Elsa.ServiceBus.Kafka;

public record KafkaTransportMessage(object? Key, object? Value, string Topic, IDictionary<string, byte[]> Headers, string? SchemaFullName = null);
