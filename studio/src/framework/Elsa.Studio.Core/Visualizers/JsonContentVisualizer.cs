using System.Diagnostics.CodeAnalysis;
using Elsa.Studio.Contracts;
using System.Text.Json;
using Elsa.Studio.Models;

namespace Elsa.Studio.Visualizers;

/// <summary>
/// Provides a Json content visualizer.
/// </summary>
public class JsonContentVisualizer : IContentVisualizer
{
    private static readonly JsonSerializerOptions IndentedJsonSerializerOptions = new() { WriteIndented = true };

    /// <inheritdoc/>
    public string Name => "Json";

    /// <inheritdoc/>
    public string Syntax => "json";

    /// <inheritdoc/>
    public bool CanVisualize(object input)
    {
        if (input is not string str) return false;
        str = str.Trim();
        return str.StartsWith("{") || str.StartsWith("[");
    }

    /// <inheritdoc/>
    public string? ToPretty(object input)
    {
        try
        {
            if (input is not string str)
                return input?.ToString() ?? string.Empty;

            using var document = JsonDocument.Parse(str);
            return FormatJsonElement(document.RootElement);
        }
        catch
        {
            return input?.ToString() ?? string.Empty;
        }
    }

    /// <inheritdoc/>
    public TabulatedContentVisualizer? ToTable(object input)
    {
        if (input is not string str) return null;

        try
        {
            using var doc = JsonDocument.Parse(str);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return null;

            var rows = new List<IReadOnlyList<string>>();
            var headers = new HashSet<string>();

            // First pass: collect all unique property names
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) return null;
                foreach (var prop in item.EnumerateObject())
                {
                    headers.Add(prop.Name);
                }
            }

            var headerList = headers.ToList();

            // Second pass: build rows in header order
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var row = new List<string>();
                foreach (var header in headerList)
                {
                    if (item.TryGetProperty(header, out var val))
                        row.Add(JsonElementToString(val));
                    else
                        row.Add(""); // missing value
                }
                rows.Add(row);
            }

            return new TabulatedContentVisualizer
            {
                Headers = headerList,
                Rows = rows
            };
        }
        catch
        {
            return null;
        }
    }

    private string JsonElementToString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            _ => FormatJsonElement(element)
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "JsonElement serialization does not require runtime type discovery.")]
    private static string FormatJsonElement(JsonElement element) => JsonSerializer.Serialize(element, IndentedJsonSerializerOptions);
}
