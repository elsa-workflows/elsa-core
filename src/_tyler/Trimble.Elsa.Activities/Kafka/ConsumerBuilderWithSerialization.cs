using Avro.Specific;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;

namespace Trimble.Elsa.Activities.Kafka;

public class ConsumerBuilderWithSerialization<TK, TV> : ConsumerBuilder<TK, TV>
{
    CachedSchemaRegistryClient? _schemaRegistryClient;
    public ConsumerBuilderWithSerialization(IEnumerable<KeyValuePair<string, string>> config) : base(config)
    {
    }

    public ConsumerBuilderWithSerialization<TK, TV> SetKeyValueDeserializers(CachedSchemaRegistryClient schemaRegistryClient)
    {
        _schemaRegistryClient = schemaRegistryClient;
        SetKeyDeserializer();
        SetValueDeserializer();

        return this;
    }

    public ConsumerBuilderWithSerialization<TK, TV> SetKeyDeserializer()
    {
        if (!typeof(Null).IsAssignableFrom(typeof(TK)))
            return this;

        var simpleKeyDeserializer = SimpleDeserializerFactory<TK>();
        if (simpleKeyDeserializer != null)
        {
            SetKeyDeserializer(simpleKeyDeserializer);
        }
        else if (typeof(ISpecificRecord).IsAssignableFrom(typeof(TK)))
        {
            if (_schemaRegistryClient == null)
                throw new InvalidOperationException("Schema registry client is required for Avro consumer.");

            var registeredSerializer = new AvroDeserializer<TK>(_schemaRegistryClient);

            if (registeredSerializer != null)
            {
                SetKeyDeserializer(registeredSerializer.AsSyncOverAsync());
            }
        }
        else
        {
            SetKeyDeserializer(new JsonDeserializer<TK>());
        }

        return this;
    }


    public ConsumerBuilderWithSerialization<TK, TV> SetValueDeserializer()
    {
        var simpleValueSerializer = SimpleDeserializerFactory<TV>();
        if (simpleValueSerializer != null)
        {
            SetValueDeserializer(simpleValueSerializer);
        }
        else if (typeof(ISpecificRecord).IsAssignableFrom(typeof(TV)))
        {
            var registeredDeserializer = new AvroDeserializer<TV>(_schemaRegistryClient);

            if (registeredDeserializer != null)
            {
                SetValueDeserializer(registeredDeserializer.AsSyncOverAsync());
            }
        }
        else
        {
            SetValueDeserializer(new JsonDeserializer<TV>());
        }

        return this;
    }

    private IDeserializer<T> SimpleDeserializerFactory<T>()
    {

        if (typeof(T) == typeof(string))
        {
            return (IDeserializer<T>)Deserializers.Utf8;
        }
        else if (typeof(T) == typeof(Null))
        {
            return (IDeserializer<T>)Deserializers.Null;
        }
        else if (typeof(T) == typeof(long))
        {
            return (IDeserializer<T>)Deserializers.Int64;
        }
        else if (typeof(T) == typeof(int))
        {
            return (IDeserializer<T>)Deserializers.Int64;
        }
        else if (typeof(T) == typeof(float))
        {
            return (IDeserializer<T>)Deserializers.Single;
        }
        else if (typeof(T) == typeof(double))
        {
            return (IDeserializer<T>)Deserializers.Double;
        }
        else if (typeof(T) == typeof(byte[]))
        {
            return (IDeserializer<T>)Deserializers.ByteArray;
        }
        else
        {
            return null;
        }

    }
}
