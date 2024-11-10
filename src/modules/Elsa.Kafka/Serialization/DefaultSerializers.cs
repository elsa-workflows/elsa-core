using System.Text.Json;
using Elsa.Workflows.Serialization.Converters;

namespace Elsa.Kafka.Serialization;

public static class DefaultSerializers
{
    private static JsonSerializerOptions _serializerOptions = GetOptions();
    
    private static JsonSerializerOptions GetOptions()
    {
        var options = new JsonSerializerOptions
        {
            Converters =
            {
                new ExpandoObjectConverterFactory()
            }
        };

        return options;
    }
    
    public static string PayloadSerializer(IServiceProvider serviceProvider, object obj)
    {
        var json = JsonSerializer.Serialize(obj, obj.GetType(), _serializerOptions);
        return json;
    }

    public static object PayloadDeserializer(IServiceProvider serviceProvider, string json, Type type)
    {
        var value = JsonSerializer.Deserialize(json, type, _serializerOptions)!;
        return value;
    }
}