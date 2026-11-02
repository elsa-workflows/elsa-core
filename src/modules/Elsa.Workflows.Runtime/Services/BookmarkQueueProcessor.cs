using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Processes queued stimuli, retaining undelivered items for a later pass.
/// </summary>
public class BookmarkQueueProcessor(IBookmarkQueueStore store, IWorkflowResumer workflowResumer, ILogger<BookmarkQueueProcessor> logger) : IBookmarkQueueProcessor
{
    /// <inheritdoc />
    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        var batchSize = 50;
        var offset = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var pageArgs = PageArgs.FromRange(offset, batchSize);
            var page = await store.PageAsync(pageArgs, new BookmarkQueueItemOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending), cancellationToken);

            var retainedCount = await ProcessPageAsync(page, cancellationToken);

            if (page.Items.Count < batchSize)
                break;

            offset += retainedCount;
        }
    }

    /// <summary>
    /// Processes a page and returns the number of items still occupying positions in the queue.
    /// </summary>
    private async Task<int> ProcessPageAsync(Page<BookmarkQueueItem> page, CancellationToken cancellationToken = default)
    {
        var retainedCount = 0;

        foreach (var bookmarkQueueItem in page.Items)
        {
            if (await ProcessItemAsync(bookmarkQueueItem, cancellationToken))
            {
                retainedCount++;
            }
        }

        return retainedCount;
    }

    /// <summary>
    /// Attempts delivery and returns whether the item remains in the queue.
    /// </summary>
    private async Task<bool> ProcessItemAsync(BookmarkQueueItem item, CancellationToken cancellationToken = default)
    {
        var filter = item.CreateBookmarkFilter();
        var options = item.Options;
        
        logger.LogDebug("Processing bookmark queue item {BookmarkQueueItemId} for workflow instance {WorkflowInstanceId} for activity type {ActivityType}", item.Id, item.WorkflowInstanceId, item.ActivityTypeName);
        
        List<RunWorkflowInstanceResponse> responses;

        try
        {
            responses = (await workflowResumer.ResumeAsync(filter, options, cancellationToken)).ToList();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            logger.LogWarning(exception, "Failed to resume bookmark queue item {BookmarkQueueItemId}. The item is retained for a later attempt.", item.Id);
            return true;
        }

        if (responses.Count > 0)
        {
            logger.LogDebug("Successfully resumed {WorkflowCount} workflow instances using stimulus {StimulusHash} for activity type {ActivityType}", responses.Count, item.StimulusHash, item.ActivityTypeName);
            await store.DeleteAsync(item.Id, cancellationToken);
            return false;
        }
        else
        {
            logger.LogDebug("No matching bookmarks found for bookmark queue item {BookmarkQueueItemId} for workflow instance {WorkflowInstanceId} for activity type {ActivityType} with stimulus {StimulusHash}", item.Id, item.WorkflowInstanceId, item.ActivityTypeName, item.StimulusHash);
        }

        return true;
    }
}
