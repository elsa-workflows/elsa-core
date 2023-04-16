using Elsa.Workflows.Runtime.Entities;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extensions to <see cref="StoredBookmark"/>.
/// </summary>
public static class StoredBookmarkExtensions
{
    /// <summary>
    /// Returns the Data property of the stored bookmark as a strongly-typed object.
    /// </summary>
    /// <param name="storedBookmark">The stored bookmark.</param>
    /// <typeparam name="T">The type of the Data property.</typeparam>
    /// <returns>The Data property of the stored bookmark as a strongly-typed object.</returns>
    public static T GetPayload<T>(this StoredBookmark storedBookmark) => (T)storedBookmark.Payload!;
}