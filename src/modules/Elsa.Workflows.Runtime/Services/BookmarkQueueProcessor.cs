using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime;

public class BookmarkQueueProcessor(IBookmarkQueueStore store, IWorkflowResumer workflowResumer, ILogger<BookmarkQueueProcessor> logger) : IBookmarkQueueProcessor
{
    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        var batchSize = 50;
        var offset = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var pageArgs = PageArgs.FromRange(offset, batchSize);
            var page = await store.PageAsync(pageArgs, new BookmarkQueueItemOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending), cancellationToken);

            await ProcessPageAsync(page, cancellationToken);

            if (page.Items.Count < batchSize)
                break;

            offset += batchSize;
        }
    }

    private async Task ProcessPageAsync(Page<BookmarkQueueItem> page, CancellationToken cancellationToken = default)
    {
        foreach (var bookmarkQueueItem in page.Items)
        {
            try
            {
                await ProcessItemAsync(bookmarkQueueItem, cancellationToken);
            }
            catch (Exception ex)
            {
                // Log the error but continue processing other items in the batch
                logger.LogError(ex, "Error processing bookmark queue item {BookmarkQueueItemId}. Continuing with next item.", bookmarkQueueItem.Id);
            }
        }
    }

    private async Task ProcessItemAsync(BookmarkQueueItem item, CancellationToken cancellationToken = default)
    {
        var filter = item.CreateBookmarkFilter();
        var options = item.Options;
        
        logger.LogDebug("Processing bookmark queue item {BookmarkQueueItemId} for workflow instance {WorkflowInstanceId} for activity type {ActivityType}", item.Id, item.WorkflowInstanceId, item.ActivityTypeName);
        
        try
        {
            var responses = (await workflowResumer.ResumeAsync(filter, options, cancellationToken)).ToList();

            if (responses.Count > 0)
            {
                logger.LogDebug("Successfully resumed {WorkflowCount} workflow instances using stimulus {StimulusHash} for activity type {ActivityType}", responses.Count, item.StimulusHash, item.ActivityTypeName);
            }
            else
            {
                logger.LogDebug("No matching bookmarks found for bookmark queue item {BookmarkQueueItemId} for workflow instance {WorkflowInstanceId} for activity type {ActivityType} with stimulus {StimulusHash}. The bookmark may have already been processed by another queue item.", item.Id, item.WorkflowInstanceId, item.ActivityTypeName, item.StimulusHash);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resuming workflow for bookmark queue item {BookmarkQueueItemId} for workflow instance {WorkflowInstanceId}. The queue item will still be deleted to prevent accumulation.", item.Id, item.WorkflowInstanceId);
        }
        finally
        {
            // Always delete the queue item after processing, regardless of whether bookmarks were found or an exception occurred.
            // This prevents duplicate queue items from accumulating when the same bookmark is queued multiple times in rapid succession.
            // The distributed lock in WorkflowResumer ensures that the actual bookmark is only processed once.
            // Use CancellationToken.None to ensure cleanup happens even during application shutdown.
            try
            {
                await store.DeleteAsync(item.Id, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete bookmark queue item {BookmarkQueueItemId}. It will be purged by the recurring purge task.", item.Id);
            }
        }
    }
}