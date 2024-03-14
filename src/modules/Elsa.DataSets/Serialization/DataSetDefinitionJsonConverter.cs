using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.DataSets.Contracts;
using Elsa.DataSets.Entities;

namespace Elsa.DataSets.Serialization;

/// <summary>
/// Converts a <see cref="DataSetDefinition"/> to and from JSON.
/// </summary>
public class DataSetDefinitionJsonConverter : JsonConverter<DataSetDefinition>
{
    /// <inheritdoc />
    [RequiresUnreferencedCode("The type being converted must be preserved.")]
    public override DataSetDefinition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonDocument = JsonDocument.ParseValue(ref reader);
        var root = jsonDocument.RootElement;
        var name = root.GetProperty("name").GetString()!;
        var id = root.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
        var typeName = root.GetProperty("type").GetString()!;
        var type = Type.GetType(typeName)!;
        var properties = root.GetProperty("properties").GetRawText();
        var dataSet = (IDataSet)JsonSerializer.Deserialize(properties, type, options)!;
        var createdAt = root.GetProperty("createdAt").GetDateTimeOffset();
        var updatedAt = root.GetProperty("updatedAt").GetDateTimeOffset();
        var description = root.GetProperty("description").GetString();
        return new DataSetDefinition
        {
            Id = id,
            Name = name,
            DataSet = dataSet,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Description = description
        };
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type being converted must be preserved.")]
    public override void Write(Utf8JsonWriter writer, DataSetDefinition value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("id", value.Id);
        writer.WriteString("name", value.Name);
        writer.WriteString("type", value.DataSet.GetType().AssemblyQualifiedName);
        writer.WritePropertyName("properties");
        JsonSerializer.Serialize(writer, value.DataSet, value.DataSet.GetType(), options);
        writer.WriteString("createdAt", value.CreatedAt);
        writer.WriteString("updatedAt", value.UpdatedAt);
        writer.WriteString("description", value.Description);
        writer.WriteEndObject();
    }
}