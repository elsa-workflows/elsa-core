using System.Text.Json;
using Confluent.Kafka;

namespace Elsa.ServiceBus.Kafka.Serializers;

public class JsonDeserializer<T> : IDeserializer<T>
{
    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        return JsonSerializer.Deserialize<T>(data)!;
    }
}