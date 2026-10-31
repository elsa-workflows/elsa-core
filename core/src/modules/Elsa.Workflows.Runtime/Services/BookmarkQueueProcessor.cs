using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Common;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime;

public class BookmarkQueueProcessor(
    IBookmarkQueueStore store,
    IBookmarkQueueDeadLetterManager deadLetterManager,
    IWorkflowResumer workflowResumer,
    ISystemClock systemClock,
    IOptions<BookmarkQueuePurgeOptions> options,
    ILogger<BookmarkQueueProcessor> logger) : IBookmarkQueueProcessor
{
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

    private async Task<int> ProcessPageAsync(Page<BookmarkQueueItem> page, CancellationToken cancellationToken = default)
    {
        var retainedCount = 0;

        foreach (var bookmarkQueueItem in page.Items)
        {
            if (await ProcessItemAsync(bookmarkQueueItem, cancellationToken))
                retainedCount++;
        }

        return retainedCount;
    }

    private async Task<bool> ProcessItemAsync(BookmarkQueueItem item, CancellationToken cancellationToken = default)
    {
        var filter = item.CreateBookmarkFilter();
        var resumeOptions = item.Options;
        
        logger.LogDebug("Processing bookmark queue item {BookmarkQueueItemId} for workflow instance {WorkflowInstanceId} for activity type {ActivityType}", item.Id, item.WorkflowInstanceId, item.ActivityTypeName);

        List<RunWorkflowInstanceResponse> responses;

        try
        {
            responses = (await workflowResumer.ResumeAsync(filter, resumeOptions, cancellationToken)).ToList();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            return await HandleFailureAsync(item, ex, cancellationToken);
        }

        if (responses.Count > 0)
        {
            logger.LogDebug("Successfully resumed {WorkflowCount} workflow instances using stimulus {StimulusHash} for activity type {ActivityType}", responses.Count, item.StimulusHash, item.ActivityTypeName);
            await store.DeleteAsync(item.Id, cancellationToken);
            return false;
        }

        logger.LogDebug("No matching bookmarks found for bookmark queue item {BookmarkQueueItemId} for workflow instance {WorkflowInstanceId} for activity type {ActivityType} with stimulus {StimulusHash}", item.Id, item.WorkflowInstanceId, item.ActivityTypeName, item.StimulusHash);
        return true;
    }

    private async Task<bool> HandleFailureAsync(BookmarkQueueItem item, Exception exception, CancellationToken cancellationToken)
    {
        item.DeliveryAttempts++;
        item.LastAttemptedAt = systemClock.UtcNow;
        item.LastErrorType = exception.GetType().FullName;
        item.LastErrorMessage = exception.Message;

        if (item.DeliveryAttempts < options.Value.MaxDeliveryAttempts)
        {
            logger.LogWarning(
                exception,
                "Failed to process bookmark queue item {BookmarkQueueItemId}. Attempt {DeliveryAttempt} of {MaxDeliveryAttempts}.",
                item.Id,
                item.DeliveryAttempts,
                options.Value.MaxDeliveryAttempts);

            await store.SaveAsync(item, cancellationToken);
            return true;
        }

        logger.LogError(
            exception,
            "Moving bookmark queue item {BookmarkQueueItemId} to dead letter after {DeliveryAttempt} failed delivery attempts.",
            item.Id,
            item.DeliveryAttempts);

        await deadLetterManager.DeadLetterAsync(item, "Failed", exception, cancellationToken);
        await store.DeleteAsync(item.Id, cancellationToken);
        return false;
    }
}
