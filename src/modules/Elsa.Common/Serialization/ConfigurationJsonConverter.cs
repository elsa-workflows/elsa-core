using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace Elsa.Common.Serialization;

public class ConfigurationJsonConverter : JsonConverter<IConfiguration>
{
    public override IConfiguration? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var items =  JsonSerializer.Deserialize<List<KeyValuePairModel>>(ref reader)!;
            var dictionary = new Dictionary<string, string?>();
            foreach (var item in items) dictionary[item.Key] = item.Value;

            return new ConfigurationBuilder().AddInMemoryCollection(dictionary).Build();
        }
            
        // Read JSON element.
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
        var json = JsonSerializer.Serialize(value.AsEnumerable(), options);
        var jsonElement = JsonDocument.Parse(json).RootElement;
        jsonElement.WriteTo(writer);
    }
}

public class KeyValuePairModel
{
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;
}