namespace Elsa.ServiceBus.Kafka.Implementations;

/// <summary>
/// Wraps a Confluent <see cref="Confluent.Kafka.IConsumer{TKey,TValue}"/> and carries an optional
/// <see cref="ValueTransformer"/> that <see cref="Worker{TKey,TValue}"/> applies to each consumed
/// message value before publishing it in a <see cref="KafkaTransportMessage"/>.
/// The transformer also returns an optional schema full name so that Avro schema identity is
/// preserved as first-class metadata on the message rather than being derived from the value type.
/// </summary>
public record ConsumerProxy(object Consumer, Func<object?, (object? Value, string? SchemaFullName)>? ValueTransformer = null) : IConsumer;
