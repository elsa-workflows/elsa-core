using Avro.Specific;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;

namespace Trimble.Elsa.Activities.Kafka;

public class ProducerBuilderWithSerialization<TK, TV> : ProducerBuilder<TK, TV>
{
    CachedSchemaRegistryClient? _schemaRegistryClient;
    public ProducerBuilderWithSerialization(IEnumerable<KeyValuePair<string, string>> config) : base(config)
    {
    }

    public ProducerBuilderWithSerialization<TK, TV> SetKeyValueSerializers(CachedSchemaRegistryClient schemaRegistryClient)
    {
        _schemaRegistryClient = schemaRegistryClient;
        SetKeySerializer();
        SetValueSerializer();

        return this;
    }

    private ProducerBuilderWithSerialization<TK, TV> SetKeySerializer()
    {
        if (!typeof(Null).IsAssignableFrom(typeof(TK)))
            return this;

        var simpleKeySerializer = SimpleSerializerFactory<TK>();
        if (simpleKeySerializer != null)
        {
            SetKeySerializer(simpleKeySerializer);
        }
        else if (typeof(ISpecificRecord).IsAssignableFrom(typeof(TK)))
        {
            if (_schemaRegistryClient == null)
                throw new InvalidOperationException("Schema registry client is required for Avro producer.");

            var registeredSerializer = new AvroSerializer<TK>(_schemaRegistryClient, new() { AutoRegisterSchemas = false }); 

            if (registeredSerializer != null)
            {
                SetKeySerializer(registeredSerializer);
            }
        }
        else
        {
            SetKeySerializer(new JsonSerializer<TK>());
        }

        return this;
    }

    private ProducerBuilderWithSerialization<TK, TV> SetValueSerializer()
    {
        var simpleValueSerializer = SimpleSerializerFactory<TV>();
        if (simpleValueSerializer != null)
        {
            SetValueSerializer(simpleValueSerializer);
        }
        else if (typeof(ISpecificRecord).IsAssignableFrom(typeof(TV)))
        {
            var registeredSerializer = new AvroSerializer<TV>(_schemaRegistryClient, new() { AutoRegisterSchemas = false }); ;

            if (registeredSerializer != null)
            {
                SetValueSerializer(registeredSerializer);
            }
        }
        else
        {
            SetValueSerializer(new JsonSerializer<TV>());
        }

        return this;
    }

    private ISerializer<T> SimpleSerializerFactory<T>()
    {

        if (typeof(T) == typeof(string))
        {
            return (ISerializer<T>)Serializers.Utf8;
        }
        else if (typeof(T) == typeof(Null))
        {
            return (ISerializer<T>)Serializers.Null;
        }
        else if (typeof(T) == typeof(long))
        {
            return (ISerializer<T>)Serializers.Int64;
        }
        else if (typeof(T) == typeof(int))
        {
            return (ISerializer<T>)Serializers.Int64;
        }
        else if (typeof(T) == typeof(float))
        {
            return (ISerializer<T>)Serializers.Single;
        }
        else if (typeof(T) == typeof(double))
        {
            return (ISerializer<T>)Serializers.Double;
        }
        else if (typeof(T) == typeof(byte[]))
        {
            return (ISerializer<T>)Serializers.ByteArray;
        }
        else
        {
            return null!;
        }

    }
}
