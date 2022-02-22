using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Services;
using Rebus.Extensions;

namespace Elsa;

public static class BookmarkExtensions
{
    public static IEnumerable<Bookmark> FilterByType<T>(this IEnumerable<Bookmark> bookmarks) where T:IBookmark
    {
        var typeName = typeof(T).GetSimpleAssemblyQualifiedName();
        return bookmarks.Where(x => x.ModelType == typeName);
    }
}