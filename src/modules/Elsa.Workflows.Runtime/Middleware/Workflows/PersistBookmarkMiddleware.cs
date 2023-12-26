using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime.Contracts;

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
        await _bookmarksPersister.PersistBookmarksAsync(context, diff);
    }
}