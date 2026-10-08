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

    /// <summary>
    /// Clones the options.
    /// </summary>
    public static JsonSerializerOptions Clone(this JsonSerializerOptions options)
    {
        return new(options);
    }

    /// <summary>
    /// Clones the options and sets <see cref="ReferenceHandler.Preserve"/> for execution-time value conversion.
    /// </summary>
    public static JsonSerializerOptions CloneForValueConversion(this JsonSerializerOptions options)
    {
        var clone = options.Clone();
        clone.ReferenceHandler = ReferenceHandler.Preserve;
        return clone;
    }
}