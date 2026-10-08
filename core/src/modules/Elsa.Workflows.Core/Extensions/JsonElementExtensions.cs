using System.Text.Json;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class JsonElementExtensions
{
    /// <summary>
    /// Returns a child element where its name matches any of the specified names.
    /// </summary>
    public static JsonElement GetProperty(this JsonElement element, params string[] names)
    {
        foreach (var name in names)
            if (element.TryGetProperty(name, out var value))
                return value;

        throw new KeyNotFoundException($"None of the specified keys were found: {string.Join(", ", names)}");
    }
}