using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.DataSets.Contracts;
using Elsa.DataSets.Entities;

namespace Elsa.DataSets.Serialization;

/// <summary>
/// Converts a <see cref="LinkedServiceDefinition"/> to and from JSON.
/// </summary>
public class LinkedServiceDefinitionJsonConverter : JsonConverter<LinkedServiceDefinition>
{
    /// <inheritdoc />
    [RequiresUnreferencedCode("The type being converted must be preserved.")]
    public override LinkedServiceDefinition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonDocument = JsonDocument.ParseValue(ref reader);
        var root = jsonDocument.RootElement;
        var name = root.GetProperty("name").GetString()!;
        var id = root.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
        var typeName = root.GetProperty("type").GetString()!;
        var type = Type.GetType(typeName)!;
        var properties = root.GetProperty("properties").GetRawText();
        var linkedService = (ILinkedService)JsonSerializer.Deserialize(properties, type, options)!;
        var createdAt = root.GetProperty("createdAt").GetDateTimeOffset();
        var updatedAt = root.GetProperty("updatedAt").GetDateTimeOffset();
        var description = root.GetProperty("description").GetString();
        return new LinkedServiceDefinition()
        {
            Id = id,
            Name = name,
            LinkedService = linkedService,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Description = description
        };
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type being converted must be preserved.")]
    public override void Write(Utf8JsonWriter writer, LinkedServiceDefinition value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("id", value.Id);
        writer.WriteString("name", value.Name);
        writer.WriteString("type", value.LinkedService.GetType().AssemblyQualifiedName);
        writer.WritePropertyName("properties");
        JsonSerializer.Serialize(writer, value.LinkedService, value.LinkedService.GetType(), options);
        writer.WriteString("createdAt", value.CreatedAt);
        writer.WriteString("updatedAt", value.UpdatedAt);
        writer.WriteString("description", value.Description);
        writer.WriteEndObject();
    }
}