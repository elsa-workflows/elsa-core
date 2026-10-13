using System.Text.Json;
using Elsa.Workflows.Serialization.Serializers;

namespace Elsa.Workflows.Serialization.Helpers;

/// <summary>
/// Converts a payload that was reloaded from a store into its strongly-typed form.
/// A payload can come back untyped (for example as an ExpandoObject or a JsonElement);
/// such a payload is converted to <typeparamref name="T"/> using the payload serializer's base options.
/// </summary>
public static class PayloadConverter
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
