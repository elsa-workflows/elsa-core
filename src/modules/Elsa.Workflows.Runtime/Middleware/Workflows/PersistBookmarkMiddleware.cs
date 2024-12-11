using Elsa.Workflows.Helpers;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

/// <summary>
/// Takes care of loading and persisting bookmarks.
/// </summary>
public class PersistBookmarkMiddleware : WorkflowExecutionMiddleware
{
    private readonly IBookmarksPersister _bookmarksPersister;

    /// <inheritdoc />
    public PersistBookmarkMiddleware(WorkflowMiddlewareDelegate next, IBookmarksPersister bookmarksPersister) : base(next)
    {
        _bookmarksPersister = bookmarksPersister;
    }

    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        var originalBookmarks = context.Bookmarks.ToList();
        await Next(context);
        var updatedBookmarks = context.Bookmarks.ToList();
        var diff = Diff.For(originalBookmarks, updatedBookmarks);
        var bookmarkRequest = new UpdateBookmarksRequest(context, context.Id, diff, context.CorrelationId);
        await _bookmarksPersister.PersistBookmarksAsync(bookmarkRequest);
    }
}