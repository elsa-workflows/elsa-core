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
        var results = await DeadLetterManyAsync([item], reason, exception, cancellationToken);
        return results.Single();
    }

    public async Task<IReadOnlyCollection<BookmarkQueueDeadLetterItem>> DeadLetterManyAsync(IEnumerable<BookmarkQueueItem> items, string reason, Exception? exception = null, CancellationToken cancellationToken = default)
    {
        var itemList = items.ToList();

        if (itemList.Count == 0)
            return Array.Empty<BookmarkQueueDeadLetterItem>();

        var originalQueueItemIds = itemList.Select(x => x.Id).ToList();
        var existingDeadLetters = (await deadLetterStore.FindManyAsync(new BookmarkQueueDeadLetterFilter { OriginalQueueItemIds = originalQueueItemIds }, cancellationToken)).ToList();
        var existingByOriginalQueueItemId = existingDeadLetters.ToDictionary(x => x.OriginalQueueItemId);
        var now = systemClock.UtcNow;
        var deadLetterItems = itemList
            .Where(item => !existingByOriginalQueueItemId.ContainsKey(item.Id))
            .Select(item => CreateDeadLetterItem(item, reason, exception, now))
            .ToList();

        if (deadLetterItems.Count == 0)
            return itemList.Select(x => existingByOriginalQueueItemId[x.Id]).ToList();

        var savedDeadLetterItems = await deadLetterStore.AddOrGetExistingManyAsync(deadLetterItems, cancellationToken);
        var savedByOriginalQueueItemId = savedDeadLetterItems.ToDictionary(x => x.OriginalQueueItemId);
        var newDeadLetterIds = deadLetterItems.Select(x => x.Id).ToHashSet();

        foreach (var savedDeadLetterItem in savedDeadLetterItems.Where(x => newDeadLetterIds.Contains(x.Id)))
        {
            logger.LogInformation(
                "Moved bookmark queue item {BookmarkQueueItemId} to dead letter {BookmarkQueueDeadLetterItemId} because {Reason}.",
                savedDeadLetterItem.OriginalQueueItemId,
                savedDeadLetterItem.Id,
                reason);
        }

        return itemList.Select(x => existingByOriginalQueueItemId.GetValueOrDefault(x.Id) ?? savedByOriginalQueueItemId[x.Id]).ToList();
    }

    private BookmarkQueueDeadLetterItem CreateDeadLetterItem(BookmarkQueueItem item, string reason, Exception? exception, DateTimeOffset now)
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
            DeadLetteredAt = now,
            Reason = reason,
            DeliveryAttempts = item.DeliveryAttempts,
            LastAttemptedAt = item.LastAttemptedAt,
            LastErrorType = exception?.GetType().FullName ?? item.LastErrorType,
            LastErrorMessage = exception?.Message ?? item.LastErrorMessage,
            CanReplay = true
        };

        return deadLetterItem;
    }

    public async Task<ReplayBookmarkQueueDeadLetterResult> ReplayAsync(string id, CancellationToken cancellationToken = default)
    {
        var queueItemId = identityGenerator.GenerateId();
        var replayedAt = systemClock.UtcNow;
        var item = await deadLetterStore.TryMarkReplayedAsync(id, queueItemId, replayedAt, cancellationToken);

        if (item == null)
        {
            var existing = await deadLetterStore.FindAsync(new BookmarkQueueDeadLetterFilter { Id = id }, cancellationToken);
            return existing == null
                ? new(false, null, ReplayBookmarkQueueDeadLetterResult.ReasonNotFound)
                : new(false, existing.ReplayedQueueItemId, ReplayBookmarkQueueDeadLetterResult.ReasonNotReplayable);
        }

        var queueItem = new BookmarkQueueItem
        {
            Id = queueItemId,
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

        try
        {
            await bookmarkQueueStore.AddAsync(queueItem, cancellationToken);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            item.CanReplay = true;
            item.ReplayedAt = null;
            item.ReplayedQueueItemId = null;
            await deadLetterStore.SaveAsync(item, CancellationToken.None);

            logger.LogWarning(
                ex,
                "Failed to enqueue replayed bookmark queue item {BookmarkQueueItemId}; restored dead-letter item {BookmarkQueueDeadLetterItemId} for replay.",
                queueItem.Id,
                item.Id);

            throw;
        }

        await bookmarkQueueSignaler.TriggerAsync(cancellationToken);

        logger.LogInformation(
            "Replayed bookmark queue dead-letter item {BookmarkQueueDeadLetterItemId} as queue item {BookmarkQueueItemId}.",
            item.Id,
            queueItem.Id);

        return new(true, queueItem.Id, null);
    }
}
