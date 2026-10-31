using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elsa.Http.ContentWriters;

/// <summary>
/// Reads form fields from the objects HTTP activities already accept as request content.
/// </summary>
internal static class FormContentReader
{
    public static IDictionary<string, string> ReadStrings(object content)
    {
        var fields = new Dictionary<string, string>();
        foreach (var (name, value) in ReadValues(content))
            fields[name] = ToFormString(value);

        return fields;
    }

    public static IReadOnlyList<KeyValuePair<string, object?>> ReadValues(object content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var fields = new List<KeyValuePair<string, object?>>();

        switch (content)
        {
            // JavaScript expressions evaluate to ExpandoObject, which implements
            // IDictionary<string, object?> and not the non-generic IDictionary.
            // Copy the pairs unchanged so byte arrays and files stay file parts.
            case IDictionary<string, object?> objectDictionary:
                foreach (var (name, value) in objectDictionary)
                    fields.Add(new(name, value));

                return fields;
            case IDictionary dictionary:
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (entry.Key is not string name)
                        throw new NotSupportedException("Form field names must be strings.");

                    fields.Add(new(name, entry.Value));
                }

                return fields;
            case string text:
                AppendJsonObject(fields, JsonSerializer.Deserialize<JsonElement>(text), "Form content supplied as text must be a JSON object.");
                return fields;
            default:
                AppendJsonObject(
                    fields,
                    JsonSerializer.SerializeToElement(content),
                    $"Content of type {content.GetType().Name} cannot be written as form fields. Supply an object or a JSON object string.");
                return fields;
        }
    }

    public static string ToFormString(object? value) => value switch
    {
        null => string.Empty,
        string text => text,
        JsonElement element => element.ToString(),
        JsonNode jsonNode => JsonSerializer.SerializeToElement(jsonNode).ToString(),
        _ => FormatValue(value)
    };

    private static string FormatValue(object value)
    {
        // JSON numbers are already invariant. Format CLR numbers the same way.
        switch (Type.GetTypeCode(value.GetType()))
        {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
            case TypeCode.Single:
            case TypeCode.Double:
            case TypeCode.Decimal:
                return ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
            default:
                return value.ToString() ?? string.Empty;
        }
    }

    private static void AppendJsonObject(List<KeyValuePair<string, object?>> fields, JsonElement element, string errorMessage)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new NotSupportedException(errorMessage);

        foreach (var property in element.EnumerateObject())
            fields.Add(new(property.Name, property.Value.Clone()));
    }
}
