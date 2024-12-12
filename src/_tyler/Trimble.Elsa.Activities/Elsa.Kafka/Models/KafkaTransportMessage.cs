namespace Elsa.Kafka;

public record KafkaTransportMessage(object? Key, object? Value, string Topic, IDictionary<string, byte[]> Headers);