using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Options;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class BookmarkBoundWorkflowService(IWorkflowMatcher workflowMatcher) : IBookmarkBoundWorkflowService
{
    /// <inheritdoc />
    public async Task<IEnumerable<BookmarkBoundWorkflow>> FindManyAsync(string activityTypeName, object stimulus, FindBookmarkOptions? options = null, CancellationToken cancellationToken = default)
    {
        var bookmarks = await workflowMatcher.FindBookmarksAsync(activityTypeName, stimulus, options, cancellationToken);
        return Map(bookmarks);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BookmarkBoundWorkflow>> FindManyAsync(string stimulusHash, FindBookmarkOptions? options = null, CancellationToken cancellationToken = default)
    {
        var bookmarks = await workflowMatcher.FindBookmarksAsync(stimulusHash, options, cancellationToken);
        return Map(bookmarks);
    }

    private IEnumerable<BookmarkBoundWorkflow> Map(IEnumerable<StoredBookmark> bookmarks)
    {
        var groupedBookmarks = bookmarks.GroupBy(x => x.WorkflowInstanceId);
        var bookmarkBoundWorkflows = new List<BookmarkBoundWorkflow>();

        foreach (var bookmarkGroup in groupedBookmarks)
        {
            var workflowInstanceId = bookmarkGroup.Key;
            var bookmarkBoundWorkflow = new BookmarkBoundWorkflow(workflowInstanceId, bookmarkGroup.ToList());
            bookmarkBoundWorkflows.Add(bookmarkBoundWorkflow);
        }

        return bookmarkBoundWorkflows;
    }
}