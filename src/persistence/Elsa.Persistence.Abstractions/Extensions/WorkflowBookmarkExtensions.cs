using Elsa.Contracts;
using Elsa.Helpers;
using Elsa.Persistence.Entities;

namespace Elsa.Persistence.Extensions;

public static class WorkflowBookmarkExtensions
{
    public static IEnumerable<WorkflowBookmark> Filter<T>(this IEnumerable<WorkflowBookmark> bookmarks) where T : ITrigger
    {
        var bookmarkName = TypeNameHelper.GenerateTypeName<T>();
        return bookmarks.Where(x => x.Name == bookmarkName);
    }
}