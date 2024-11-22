namespace Elsa.Kafka;

public record KafkaTransportMessage(object? Key, object? Value, IDictionary<string, byte[]> Headers);