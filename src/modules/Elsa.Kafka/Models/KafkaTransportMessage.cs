using Confluent.Kafka;

namespace Elsa.Kafka;

public record KafkaTransportMessage(object? Key, string Value, Headers Headers, Confluent.Kafka.Timestamp Timestamp);