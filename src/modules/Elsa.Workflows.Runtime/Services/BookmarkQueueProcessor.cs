using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime;

public class BookmarkQueueProcessor(IBookmarkQueueStore store, IBookmarkResumer bookmarkResumer, ILogger<BookmarkQueueProcessor> logger) : IBookmarkQueueProcessor
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
            await ProcessItemAsync(bookmarkQueueItem, cancellationToken);
    }

    private async Task ProcessItemAsync(BookmarkQueueItem item, CancellationToken cancellationToken = default)
    {
        var filter = item.CreateBookmarkFilter();
        var options = item.Options;
        
        logger.LogDebug("Processing bookmark queue item {BookmarkQueueItemId} for workflow instance {WorkflowInstanceId} for activity type {ActivityType}", item.Id, item.WorkflowInstanceId, item.ActivityTypeName);
        
        var result = await bookmarkResumer.ResumeAsync(filter, options, cancellationToken);

        if (result.Matched)
        {
            logger.LogDebug("Successfully resumed workflow instance {WorkflowInstance} using bookmark {BookmarkId} for activity type {ActivityType}", item.WorkflowInstanceId, item.BookmarkId, item.ActivityTypeName);
            await store.DeleteAsync(item.Id, cancellationToken);
        }
        else
        {
            logger.LogDebug("No matching bookmark found for bookmark queue item {BookmarkQueueItemId} for workflow instance {WorkflowInstanceId} for activity type {ActivityType}", item.Id, item.WorkflowInstanceId, item.ActivityTypeName);
        }
    }
}