using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Persistence.Entities;

namespace Elsa.Workflows.Persistence.Extensions;

public static class WorkflowBookmarkExtensions
{
    public static IEnumerable<WorkflowBookmark> Filter<T>(this IEnumerable<WorkflowBookmark> bookmarks) where T : IActivity
    {
        var bookmarkName = ActivityTypeNameHelper.GenerateTypeName<T>();
        return bookmarks.Where(x => x.Name == bookmarkName);
    }
}