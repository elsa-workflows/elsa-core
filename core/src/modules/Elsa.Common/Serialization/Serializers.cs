using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Elsa.Common.Serialization;

public static class Serializers
{
    public static IConfiguration DeserializeConfiguration(JsonElement? json)
    {
        return DeserializeConfiguration(json?.GetRawText());
    }
    
    public static IConfiguration DeserializeConfiguration(string? json)
    {
        if (json == null)
            return new ConfigurationBuilder().Build();

        return JsonSerializer.Deserialize<IConfiguration>(json, SerializerOptions.ConfigurationJsonSerializerOptions)!;
    }

    public static string SerializeConfiguration(IConfiguration configuration)
    {
        return JsonSerializer.Serialize(configuration, SerializerOptions.ConfigurationJsonSerializerOptions);
    }
}