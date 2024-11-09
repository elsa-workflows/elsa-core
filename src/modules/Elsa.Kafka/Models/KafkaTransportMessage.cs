using Confluent.Kafka;
using Timestamp = Confluent.Kafka.Timestamp;

namespace Elsa.Kafka;

public record KafkaTransportMessage(object? Key, string Value, Headers Headers, Timestamp Timestamp);