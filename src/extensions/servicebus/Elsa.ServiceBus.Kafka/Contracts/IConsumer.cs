namespace Elsa.ServiceBus.Kafka;

public interface IConsumer
{
    object Consumer { get; }

    /// <summary>
    /// Optional factory-supplied delegate that transforms a raw consumed value before it is
    /// placed into <see cref="KafkaTransportMessage"/>.  Returns the transformed value and
    /// an optional schema full name.  When <c>null</c>, the raw value is used as-is.
    /// </summary>
    Func<object?, (object? Value, string? SchemaFullName)>? ValueTransformer { get; }
}
