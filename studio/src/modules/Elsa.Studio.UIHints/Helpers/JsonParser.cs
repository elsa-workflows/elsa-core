using System.Text.Json;

namespace Elsa.Studio.UIHints.Helpers;

/// <summary>
/// Provides utility methods for parsing JSON strings into typed objects.
/// </summary>
public static class JsonParser
{
    /// <summary>
    /// Parses the specified JSON string into an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <param name="defaultValueFactory">A factory function that returns a default value of type <typeparamref name="T"/>.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use when parsing the JSON string.</param>
    /// <typeparam name="T">The type of the object to parse the JSON string into.</typeparam>
    /// <returns>An object of type <typeparamref name="T"/>.</returns>
    public static T ParseJson<T>(string? json, Func<T> defaultValueFactory, JsonSerializerOptions? options = default)
    {
        if (string.IsNullOrWhiteSpace(json))
            return defaultValueFactory();

        try
        {
            options ??= new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Deserialize<T>(json, options) ?? defaultValueFactory();
        }
        catch (JsonException)
        {
            return defaultValueFactory();
        }
    }
}