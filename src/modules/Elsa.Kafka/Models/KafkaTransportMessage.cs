namespace Elsa.Kafka;

public record KafkaTransportMessage(object? Key, string Value, IDictionary<string, byte[]> Headers);