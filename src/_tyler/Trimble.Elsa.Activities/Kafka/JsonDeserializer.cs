using Confluent.Kafka;
using System.Text.Json;

namespace Trimble.Elsa.Activities.Kafka;

public class JsonDeserializer<T> : IDeserializer<T>
{
    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        return JsonSerializer.Deserialize<T>(data);
    }
}