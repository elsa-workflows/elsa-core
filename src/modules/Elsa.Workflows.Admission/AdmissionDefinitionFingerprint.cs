using System.Text;
using System.Text.Json;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Admission;

/// <summary>Full pinned definition content. Publication flags are separate verified lifecycle facts.</summary>
public static class AdmissionDefinitionFingerprint
{
    public static string Compute(WorkflowDefinition definition, IPayloadSerializer serializer)
    {
        if (definition.CreatedAt == default || definition.CreatedAt.Offset != TimeSpan.Zero || definition.CreatedAt.UtcDateTime.Ticks % 10 != 0)
        {
            throw new ArgumentException("Pinned definition timestamps must be explicit UTC values at PostgreSQL microsecond precision.");
        }
        var element = serializer.SerializeToElement(new
        {
            definition.Id, definition.TenantId, definition.DefinitionId, definition.Version, definition.CreatedAt,
            definition.Name, definition.Description, definition.ToolVersion, definition.Options, definition.Variables,
            definition.Inputs, definition.Outputs, definition.Outcomes, definition.CustomProperties, definition.ProviderName,
            definition.MaterializerName, definition.MaterializerContext,
            StringData = definition.MaterializerName == "Json" ? JsonContent(definition.StringData) : definition.StringData,
            OriginalSource = definition.MaterializerName == "Json" ? JsonContent(definition.OriginalSource) : definition.OriginalSource,
            definition.BinaryData, definition.IsReadonly, definition.IsSystem
        });
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteCanonical(writer, element);
        }
        return AdmissionHash.Compute(Encoding.UTF8.GetString(stream.ToArray()));
    }

    private static object? JsonContent(string? value)
    {
        if (value == null)
        {
            return null;
        }
        try
        {
            using var document = JsonDocument.Parse(value);
            ValidateJson(document.RootElement);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return value;
        }
    }

    private static void ValidateJson(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    throw new ArgumentException("Pinned JSON definition content cannot contain duplicate property names.");
                }
                ValidateJson(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                ValidateJson(item);
            }
        }
    }

    private static void WriteCanonical(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteCanonical(writer, item);
                }
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }
}
