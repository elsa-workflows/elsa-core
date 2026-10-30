using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace Elsa.Common.Serialization;

public class ConfigurationJsonConverter : JsonConverter<IConfiguration>
{
    public override IConfiguration? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonElement = JsonElement.ParseValue(ref reader);
        using var memoryStream = new MemoryStream();
        using var writer = new Utf8JsonWriter(memoryStream);
        jsonElement.WriteTo(writer);
        writer.Flush();
        memoryStream.Position = 0;

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonStream(memoryStream);
        return configurationBuilder.Build();
    }

    public override void Write(Utf8JsonWriter writer, IConfiguration value, JsonSerializerOptions options)
    {
        var dictionary = new Dictionary<string, object?>();
        foreach (var child in value.GetChildren())
            dictionary[child.Key] = GetValue(child);

        JsonSerializer.Serialize(writer, dictionary, options);
    }

    private static object? GetValue(IConfigurationSection section)
    {
        var children = section.GetChildren().ToList();

        if (!children.Any())
            return section.Value;

        var dict = new Dictionary<string, object?>();
        foreach (var child in children) 
            dict[child.Key] = GetValue(child);

        return dict;
    }
}