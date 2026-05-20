using Elsa.Common;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime;

public class BookmarkQueueDeadLetterManager(
    IBookmarkQueueDeadLetterStore deadLetterStore,
    IBookmarkQueueStore bookmarkQueueStore,
    IBookmarkQueueSignaler bookmarkQueueSignaler,
    ISystemClock systemClock,
    IIdentityGenerator identityGenerator,
    ILogger<BookmarkQueueDeadLetterManager> logger) : IBookmarkQueueDeadLetterManager
{
    public async Task<BookmarkQueueDeadLetterItem> DeadLetterAsync(BookmarkQueueItem item, string reason, Exception? exception = null, CancellationToken cancellationToken = default)
    {
        var deadLetterItem = new BookmarkQueueDeadLetterItem
        {
            Id = identityGenerator.GenerateId(),
            TenantId = item.TenantId,
            OriginalQueueItemId = item.Id,
            WorkflowInstanceId = item.WorkflowInstanceId,
            CorrelationId = item.CorrelationId,
            BookmarkId = item.BookmarkId,
            StimulusHash = item.StimulusHash,
            ActivityInstanceId = item.ActivityInstanceId,
            ActivityTypeName = item.ActivityTypeName,
            Options = item.Options,
            OriginalCreatedAt = item.CreatedAt,
            DeadLetteredAt = systemClock.UtcNow,
            Reason = reason,
            DeliveryAttempts = item.DeliveryAttempts,
            LastAttemptedAt = item.LastAttemptedAt,
            LastErrorType = exception?.GetType().FullName ?? item.LastErrorType,
            LastErrorMessage = exception?.Message ?? item.LastErrorMessage,
            CanReplay = true
        };

        await deadLetterStore.AddAsync(deadLetterItem, cancellationToken);

        logger.LogInformation(
            "Moved bookmark queue item {BookmarkQueueItemId} to dead letter {BookmarkQueueDeadLetterItemId} because {Reason}.",
            item.Id,
            deadLetterItem.Id,
            reason);

        return deadLetterItem;
    }

    public async Task<ReplayBookmarkQueueDeadLetterResult> ReplayAsync(string id, CancellationToken cancellationToken = default)
    {
        var item = await deadLetterStore.FindAsync(new BookmarkQueueDeadLetterFilter { Id = id }, cancellationToken);

        if (item == null)
            return new(false, null, "NotFound");

        if (!item.CanReplay || item.ReplayedAt != null)
            return new(false, item.ReplayedQueueItemId, "NotReplayable");

        var queueItem = new BookmarkQueueItem
        {
            Id = identityGenerator.GenerateId(),
            TenantId = item.TenantId,
            WorkflowInstanceId = item.WorkflowInstanceId,
            CorrelationId = item.CorrelationId,
            BookmarkId = item.BookmarkId,
            StimulusHash = item.StimulusHash,
            ActivityInstanceId = item.ActivityInstanceId,
            ActivityTypeName = item.ActivityTypeName,
            Options = item.Options,
            CreatedAt = systemClock.UtcNow
        };

        item.ReplayedAt = systemClock.UtcNow;
        item.ReplayedQueueItemId = queueItem.Id;
        item.CanReplay = false;

        await bookmarkQueueStore.AddAsync(queueItem, cancellationToken);
        await deadLetterStore.SaveAsync(item, cancellationToken);
        await bookmarkQueueSignaler.TriggerAsync(cancellationToken);

        logger.LogInformation(
            "Replayed bookmark queue dead-letter item {BookmarkQueueDeadLetterItemId} as queue item {BookmarkQueueItemId}.",
            item.Id,
            queueItem.Id);

        return new(true, queueItem.Id, null);
    }
}
