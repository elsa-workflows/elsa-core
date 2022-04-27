using Elsa.Helpers;
using Elsa.Persistence.Entities;
using Elsa.Services;

namespace Elsa.Persistence.Extensions;

public static class WorkflowBookmarkExtensions
{
    public static IEnumerable<WorkflowBookmark> Filter<T>(this IEnumerable<WorkflowBookmark> bookmarks) where T : IActivity
    {
        var bookmarkName = ActivityTypeNameHelper.GenerateTypeName<T>();
        return bookmarks.Where(x => x.Name == bookmarkName);
    }
}