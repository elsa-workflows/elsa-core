using Elsa.Helpers;
using Elsa.Models;
using Elsa.Services;

namespace Elsa;

public static class BookmarkExtensions
{
    public static IEnumerable<Bookmark> Filter<T>(this IEnumerable<Bookmark> bookmarks) where T : IActivity
    {
        var bookmarkName = TypeNameHelper.GenerateTypeName<T>();
        return bookmarks.Where(x => x.Name == bookmarkName);
    }
}