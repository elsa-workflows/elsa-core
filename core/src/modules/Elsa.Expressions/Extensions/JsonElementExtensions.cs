using System.Text.Json;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Parses a <see cref="JsonElement"/> into a .NET object.
/// </summary>
public static class JsonElementExtensions
{
    /// <summary>
    /// Parses a <see cref="JsonElement"/> into a .NET object.
    /// </summary>
    /// <param name="jsonElement">The JSON element to parse.</param>
    /// <returns>The parsed object.</returns>
    public static object? GetValue(this JsonElement jsonElement)
    {
        return jsonElement.ValueKind switch
        {
            JsonValueKind.String => jsonElement.GetString(),
            JsonValueKind.Number => jsonElement.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Undefined => null,
            JsonValueKind.Null => null,
            JsonValueKind.Object => jsonElement.GetRawText(),
            JsonValueKind.Array => jsonElement.GetRawText(),
            _ => jsonElement.GetRawText()
        };
    }
}