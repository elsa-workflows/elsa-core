using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class BookmarkExtensions
{
    public static IEnumerable<Bookmark> Filter<T>(this IEnumerable<Bookmark> bookmarks) where T : IActivity
    {
        var bookmarkName = ActivityTypeNameHelper.GenerateTypeName<T>();
        return bookmarks.Where(x => x.Name == bookmarkName);
    }
}