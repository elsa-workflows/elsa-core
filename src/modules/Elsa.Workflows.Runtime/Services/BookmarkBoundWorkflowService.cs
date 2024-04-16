using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class BookmarkBoundWorkflowService(IWorkflowMatcher workflowMatcher, IWorkflowInstanceManager workflowInstanceManager, ILogger<BookmarkBoundWorkflowService> logger) : IBookmarkBoundWorkflowService
{
    /// <inheritdoc />
    public async Task<IEnumerable<BookmarkBoundWorkflow>> FindManyAsync(string activityTypeName, object stimulus, FindBookmarkOptions? options = null, CancellationToken cancellationToken = default)
    {
        var bookmarks = await workflowMatcher.FindBookmarksAsync(activityTypeName, stimulus, options, cancellationToken);
        return await FindManyAsync(bookmarks, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BookmarkBoundWorkflow>> FindManyAsync(string stimulusHash, FindBookmarkOptions? options = null, CancellationToken cancellationToken = default)
    {
        var bookmarks = await workflowMatcher.FindBookmarksAsync(stimulusHash, options, cancellationToken);
        return await FindManyAsync(bookmarks, cancellationToken);
    }

    private async Task<IEnumerable<BookmarkBoundWorkflow>> FindManyAsync(IEnumerable<StoredBookmark> bookmarks, CancellationToken cancellationToken = default)
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