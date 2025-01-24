using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime;

public static class ActivityExecutionContextExtensions
{
    public static async Task CreateAndCommitBookmarkAsync(this ActivityExecutionContext context, CreateBookmarkArgs args)
    {
        context.CreateBookmark(args);
        await context.CommitBookmarksAsync();
    }
    
    public static async Task CommitBookmarksAsync(this ActivityExecutionContext context)
    {
        var activityBookmarks = context.Bookmarks;
        var diff = Diff.For(context.WorkflowExecutionContext.OriginalBookmarks, activityBookmarks.ToList());
        var bookmarkRequest = new UpdateBookmarksRequest(context.WorkflowExecutionContext, diff, context.WorkflowExecutionContext.CorrelationId);
        var bookmarksPersister = context.GetRequiredService<IBookmarksPersister>();
        await bookmarksPersister.PersistBookmarksAsync(bookmarkRequest);
    }
}