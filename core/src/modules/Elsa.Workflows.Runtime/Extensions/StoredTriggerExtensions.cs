using System.Text.Json;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Serialization.Serializers;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extensions to <see cref="StoredTrigger"/>.
/// </summary>
public static class StoredTriggerExtensions
{
    private static readonly JsonSerializerOptions PayloadSerializerOptions = JsonPayloadSerializer.CreateBaseOptions();

    /// <summary>
    /// Returns the Data property of the stored trigger as a strongly-typed object.
    /// </summary>
    /// <param name="storedTrigger">The stored trigger.</param>
    /// <typeparam name="T">The type of the Data property.</typeparam>
    /// <returns>The Data property of the stored trigger as a strongly-typed object.</returns>
    /// <remarks>
    /// A payload reloaded from a trigger store can come back untyped (for example as an ExpandoObject or a JsonElement).
    /// Such a payload is converted to <typeparamref name="T"/> using the payload serializer's options.
    /// </remarks>
    public static T GetPayload<T>(this StoredTrigger storedTrigger) => storedTrigger.Payload switch
    {
        T payload => payload,
        null => default!,
        JsonElement element => element.Deserialize<T>(PayloadSerializerOptions)!,
        var payload => JsonSerializer.SerializeToElement(payload, PayloadSerializerOptions).Deserialize<T>(PayloadSerializerOptions)!
    };
}
