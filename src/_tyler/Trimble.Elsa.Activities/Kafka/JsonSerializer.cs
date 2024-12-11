using System.Text.Json;
using Confluent.Kafka;

namespace Trimble.Elsa.Activities.Kafka;

public class JsonSerializer<T> : ISerializer<T>
{
    public byte[] Serialize(T data, SerializationContext context)
    {
        return JsonSerializer.SerializeToUtf8Bytes(data);
    }
}