using Avro.Generic;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Elsa.ServiceBus.Kafka.Implementations;

namespace Elsa.ServiceBus.Kafka.Factories;

/// <summary>
/// A consumer factory that deserializes Avro-encoded messages using a Confluent Schema Registry.
/// The deserialized <see cref="GenericRecord"/> is immediately converted to a
/// <see cref="Dictionary{TKey,TValue}"/> so that the value stored in
/// <see cref="KafkaTransportMessage.Value"/> is serializable by any JSON serializer.
/// The schema full name (<see cref="GenericRecord.Schema"/>.<c>Fullname</c>) is preserved as
/// <see cref="KafkaTransportMessage.SchemaFullName"/> and is used by the schema filter on
/// <see cref="Activities.MessageReceived"/> without needing to inspect the value's runtime type.
/// </summary>
public class AvroConsumerFactory : IConsumerFactory
{
    public IConsumer CreateConsumer(CreateConsumerContext context)
    {
        var schemaRegistryDefinition = context.SchemaRegistryDefinition
            ?? throw new InvalidOperationException(
                $"{nameof(AvroConsumerFactory)} requires a {nameof(SchemaRegistryDefinition)} " +
                $"to be configured on the consumer definition.");

        ISchemaRegistryClient schemaRegistryClient = BuildSchemaRegistryClient(schemaRegistryDefinition, context.ConsumerDefinition.Config);
        IAsyncDeserializer<GenericRecord> avroDeserializer = new AvroDeserializer<GenericRecord>(schemaRegistryClient);

        var consumer = new ConsumerBuilder<string, GenericRecord>(context.ConsumerDefinition.Config)
            .SetValueDeserializer(new SyncDeserializerAdapter(avroDeserializer))
            .Build();

        return new ConsumerProxy(consumer, TransformValue);
    }

    /// <summary>
    /// Converts an Avro <see cref="GenericRecord"/> to a plain <see cref="Dictionary{TKey,TValue}"/>
    /// and returns the schema full name as metadata.  Non-Avro values are passed through unchanged.
    /// </summary>
    private static (object? Value, string? SchemaFullName) TransformValue(object? raw)
    {
        if (raw is not GenericRecord record)
            return (raw, null);

        return (ConvertGenericRecord(record), record.Schema.Fullname);
    }

    private static Dictionary<string, object?> ConvertGenericRecord(GenericRecord record)
    {
        var dict = new Dictionary<string, object?>(record.Schema.Fields.Count);
        foreach (var field in record.Schema.Fields)
            dict[field.Name] = ConvertAvroValue(record.GetValue(field.Pos));
        return dict;
    }

    private static object? ConvertAvroValue(object? value) => value switch
    {
        GenericRecord nested => ConvertGenericRecord(nested),
        GenericEnum e => e.Value,
        GenericFixed f => f.Value,
        System.Collections.IList list => list.Cast<object?>().Select(ConvertAvroValue).ToList(),
        System.Collections.IDictionary map => map.Keys.Cast<string>().ToDictionary(k => k, k => ConvertAvroValue(map[k])),
        _ => value,
    };

    /// <summary>
    /// Builds a <see cref="CachedSchemaRegistryClient"/> for the given registry definition.
    /// When <see cref="AuthCredentialsSource.SaslInherit"/> is configured, the Kafka SASL credentials
    /// are bridged into the registry config, because <see cref="CachedSchemaRegistryClient"/> resolves
    /// them from its own config dictionary rather than from the Kafka client config.
    /// </summary>
    private static ISchemaRegistryClient BuildSchemaRegistryClient(SchemaRegistryDefinition def, ConsumerConfig kafkaConfig)
    {
        var registryConfig = def.Config;

        if (registryConfig.BasicAuthCredentialsSource != AuthCredentialsSource.SaslInherit || string.IsNullOrEmpty(kafkaConfig.SaslUsername))
            return new CachedSchemaRegistryClient(registryConfig);

        var merged = registryConfig
            .Where(e => e.Key is not "sasl.username" and not "sasl.password")
            .Append(new KeyValuePair<string, string>("sasl.username", kafkaConfig.SaslUsername))
            .Append(new KeyValuePair<string, string>("sasl.password", kafkaConfig.SaslPassword ?? ""));

        return new CachedSchemaRegistryClient(merged);
    }

    /// <summary>
    /// Adapts <see cref="IAsyncDeserializer{T}"/> to the synchronous <see cref="IDeserializer{T}"/> interface
    /// expected by <see cref="ConsumerBuilder{TKey,TValue}"/>.
    /// Blocking is acceptable here because schema lookups are cached by <see cref="CachedSchemaRegistryClient"/>
    /// and the consumer runs on a dedicated background thread.
    /// </summary>
    private sealed class SyncDeserializerAdapter(IAsyncDeserializer<GenericRecord> inner) : IDeserializer<GenericRecord>
    {
        public GenericRecord Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            return inner.DeserializeAsync(data.ToArray(), isNull, context).GetAwaiter().GetResult();
        }
    }
}
