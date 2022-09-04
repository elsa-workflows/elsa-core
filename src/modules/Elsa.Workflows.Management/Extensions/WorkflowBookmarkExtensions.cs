using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Management.Extensions;

public static class WorkflowBookmarkExtensions
{
    public static IEnumerable<Bookmark> Filter<T>(this IEnumerable<Bookmark> bookmarks) where T : IActivity
    {
        var bookmarkName = ActivityTypeNameHelper.GenerateTypeName<T>();
        return bookmarks.Where(x => x.Name == bookmarkName);
    }
}