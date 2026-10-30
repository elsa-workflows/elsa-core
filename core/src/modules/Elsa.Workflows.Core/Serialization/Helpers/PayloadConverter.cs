using System.Text.Json;
using Elsa.Workflows.Serialization.Serializers;

namespace Elsa.Workflows.Serialization.Helpers;

/// <summary>
/// Converts a payload that was reloaded from a store into its strongly-typed form.
/// A payload can come back untyped (for example as an ExpandoObject or a JsonElement);
/// such a payload is converted to <typeparamref name="T"/> using the payload serializer's base options.
/// </summary>
/// <remarks>
/// Conversion uses only the payload serializer's base options, so payload types that depend on
/// converters the base options lack (for example custom or DI-registered converters) will not round trip.
/// </remarks>
internal static class PayloadConverter
{
    private static readonly JsonSerializerOptions Options = JsonPayloadSerializer.CreateBaseOptions();

    /// <summary>
    /// Returns the payload as <typeparamref name="T"/>, converting untyped shapes through the payload serializer's options.
    /// </summary>
    public static T ConvertTo<T>(object? payload) => payload switch
    {
        T typed => typed,
        null => default!,
        JsonElement element => element.Deserialize<T>(Options)!,
        var other => JsonSerializer.SerializeToElement(other, Options).Deserialize<T>(Options)!
    };
}
