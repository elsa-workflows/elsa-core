using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extensions to <c>Bookmark</c> and sets thereof.
/// </summary>
public static class BookmarkExtensions
{
    /// <summary>
    /// Filters the specified set of bookmarks by the specified activity type.
    /// </summary>
    /// <param name="bookmarks">The set of bookmarks to filter.</param>
    /// <typeparam name="T">The activity type to filter by.</typeparam>
    /// <returns>The filtered set of bookmarks.</returns>
    public static IEnumerable<Bookmark> Filter<T>(this IEnumerable<Bookmark> bookmarks) where T : IActivity
    {
        var bookmarkName = ActivityTypeNameHelper.GenerateTypeName<T>();
        return bookmarks.Where(x => x.Name == bookmarkName);
    }
    
    /// <summary>
    /// Returns the Data property of the bookmark as a strongly-typed object.
    /// </summary>
    /// <param name="bookmark">The bookmark.</param>
    /// <typeparam name="T">The type of the Data property.</typeparam>
    /// <returns>The Data property of the bookmark as a strongly-typed object.</returns>
    public static T GetPayload<T>(this Bookmark bookmark) => (T)bookmark.Payload!;
}