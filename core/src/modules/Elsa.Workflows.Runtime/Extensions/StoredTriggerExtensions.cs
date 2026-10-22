using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Serialization.Helpers;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extensions to <see cref="StoredTrigger"/>.
/// </summary>
public static class StoredTriggerExtensions
{
    /// <summary>
    /// Returns the Data property of the stored trigger as a strongly-typed object.
    /// </summary>
    /// <param name="storedTrigger">The stored trigger.</param>
    /// <typeparam name="T">The type of the Data property.</typeparam>
    /// <returns>The Data property of the stored trigger as a strongly-typed object.</returns>
    /// <remarks>
    /// A payload reloaded from a trigger store can come back untyped (for example as an ExpandoObject or a JsonElement).
    /// Such a payload is converted to <typeparamref name="T"/> by <see cref="PayloadConverter"/>.
    /// </remarks>
    public static T GetPayload<T>(this StoredTrigger storedTrigger) => PayloadConverter.ConvertTo<T>(storedTrigger.Payload);
}
