using Elsa.Workflows.Pipelines.WorkflowExecution;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

/// <summary>
/// Takes care of loading and persisting bookmarks.
/// </summary>
[Obsolete("This middleware is no longer used and will be removed in a future version. Bookmarks are now persisted through the commit state handler")]
public class PersistBookmarkMiddleware(WorkflowMiddlewareDelegate next) : WorkflowExecutionMiddleware(next)
{
    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        await Next(context);
        
        // Not used anymore.
        // var bookmarkRequest = new UpdateBookmarksRequest(context, context.BookmarksDiff, context.CorrelationId);
        // await _bookmarksPersister.PersistBookmarksAsync(bookmarkRequest);
    }
}