using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="JsonSerializerOptions"/>.
/// </summary>
public static class JsonSerializerOptionsExtensions
{
    /// <summary>
    /// Adds the specified converters to the options.
    /// </summary>
    public static JsonSerializerOptions WithConverters(this JsonSerializerOptions options, params JsonConverter[] converters)
    {
        foreach (var converter in converters)
            options.Converters.Add(converter);

        return options;
    }
}