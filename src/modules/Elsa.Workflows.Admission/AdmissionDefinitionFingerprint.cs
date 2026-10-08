using System.Text;
using System.Text.Json;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Admission;

/// <summary>Full pinned definition content. Publication flags are separate verified lifecycle facts.</summary>
public static class AdmissionDefinitionFingerprint
{
    public static string Compute(WorkflowDefinition definition, IPayloadSerializer serializer)
    {
        var element = serializer.SerializeToElement(new
        {
            definition.Id, definition.TenantId, definition.DefinitionId, definition.Version, definition.CreatedAt,
            definition.Name, definition.Description, definition.ToolVersion, definition.Options, definition.Variables,
            definition.Inputs, definition.Outputs, definition.Outcomes, definition.CustomProperties, definition.ProviderName,
            definition.MaterializerName, definition.MaterializerContext, definition.StringData, definition.OriginalSource,
            definition.BinaryData, definition.IsReadonly, definition.IsSystem
        });
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteCanonical(writer, element);
        }
        return AdmissionHash.Compute(Encoding.UTF8.GetString(stream.ToArray()));
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
