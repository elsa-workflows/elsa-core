using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Extensions;

/// <summary>
/// Contains extension methods for <see cref="Bookmark"/>.
/// </summary>
public static class BookmarkExtensions
{
    /// <summary>
    /// Converts a <see cref="Bookmark"/> to a <see cref="BookmarkInfo"/>.
    /// </summary>
    public static BookmarkInfo ToBookmarkInfo(this Bookmark source) => new()
    {
        Hash = source.Hash,
        Id = source.Id,
        Name = source.Name
    };
    
    /// <summary>
    /// Converts a collection of <see cref="Bookmark"/> to a collection of <see cref="BookmarkInfo"/>.
    /// </summary>
    public static IEnumerable<BookmarkInfo> ToBookmarkInfos(this IEnumerable<Bookmark> source)
    {
        return source.Select(ToBookmarkInfo);
    }
}