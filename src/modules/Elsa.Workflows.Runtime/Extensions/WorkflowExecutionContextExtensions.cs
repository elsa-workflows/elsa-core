using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime;

public static class WorkflowExecutionContextExtensions
{
    public static async Task CommitWorkflowStateAsync(this WorkflowExecutionContext context)
    {
        var commitStateHandler = context.GetRequiredService<ICommitStateHandler>();
        await commitStateHandler.CommitAsync(context, context.CancellationToken);
        await context.CommitBookmarksAsync();
    }
    
    public static async Task CommitBookmarksAsync(this WorkflowExecutionContext context)
    {
        var bookmarkRequest = new UpdateBookmarksRequest(context, context.BookmarksDiff, context.CorrelationId);
        var bookmarksPersister = context.GetRequiredService<IBookmarksPersister>();
        await bookmarksPersister.PersistBookmarksAsync(bookmarkRequest);
    }
}