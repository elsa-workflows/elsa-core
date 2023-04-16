using Elsa.Workflows.Runtime.Entities;

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
    public static T GetPayload<T>(this StoredTrigger storedTrigger) => (T)storedTrigger.Payload!;
}