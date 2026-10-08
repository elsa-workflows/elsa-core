using System.Text;
using System.Text.Json;
using Elsa.Workflows.Admission;

namespace Elsa.Slack.SocketMode.Events;

/// <summary>Canonicalizes provider data before typed deserialization can lose presence or number spelling.</summary>
internal static class SlackSocketJson
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    internal static JsonDocument Parse(ReadOnlyMemory<byte> bytes, int maximumBytes, int maximumDepth)
    {
        if (bytes.Length == 0 || bytes.Length > maximumBytes)
        {
            throw new InvalidDataException("Socket payload size is invalid.");
        }
        _ = StrictUtf8.GetCharCount(bytes.Span);
        var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = maximumDepth });
        try
        {
            RejectDuplicateKeys(document.RootElement);
            return document;
        }
        catch
        {
            document.Dispose();
            throw;
        }
    }

    internal static string Canonicalize(JsonElement value)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            Write(writer, value);
        }
        return StrictUtf8.GetString(stream.ToArray());
    }

    internal static string Digest(JsonElement value) => AdmissionHash.Compute(Canonicalize(value));

    private static void RejectDuplicateKeys(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    throw new InvalidDataException("Duplicate Socket JSON property.");
                }
                RejectDuplicateKeys(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                RejectDuplicateKeys(item);
            }
        }
    }

    private static void Write(Utf8JsonWriter writer, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in value.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    Write(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in value.EnumerateArray())
                {
                    Write(writer, item);
                }
                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(value.GetString());
                break;
            case JsonValueKind.Number:
                writer.WriteRawValue(value.GetRawText());
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
            default:
                throw new InvalidDataException("Unsupported Socket JSON kind.");
        }
    }
}
