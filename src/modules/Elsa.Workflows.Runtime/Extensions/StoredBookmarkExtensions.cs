using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Helpers;
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
    
    
    /// <summary>
    /// Filters the specified set of stored bookmarks by the specified activity type.
    /// </summary>
    /// <param name="bookmarks">The set of stored bookmarks to filter.</param>
    /// <typeparam name="T">The activity type to filter by.</typeparam>
    /// <returns>The filtered set of stored bookmarks.</returns>
    public static IEnumerable<StoredBookmark> Filter<T>(this IEnumerable<StoredBookmark> bookmarks) where T : IActivity
    {
        var bookmarkName = ActivityTypeNameHelper.GenerateTypeName<T>();
        return bookmarks.Where(x => x.ActivityTypeName == bookmarkName);
    }
}