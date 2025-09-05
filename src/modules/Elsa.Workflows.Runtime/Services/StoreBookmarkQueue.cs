using Elsa.Common;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime;

public class StoreBookmarkQueue(
    IBookmarkQueueStore store, 
    IBookmarkQueueSignaler bookmarkQueueSignaler, 
    ISystemClock systemClock, 
    IIdentityGenerator identityGenerator,
    ILogger<StoreBookmarkQueue> logger) : IBookmarkQueue
{
    public async Task EnqueueAsync(NewBookmarkQueueItem item, CancellationToken cancellationToken = default)
    {
        var entity = new BookmarkQueueItem
        {
            Id = identityGenerator.GenerateId(),
            WorkflowInstanceId = item.WorkflowInstanceId,
            BookmarkId = item.BookmarkId,
            CorrelationId = item.CorrelationId,
            StimulusHash = item.StimulusHash,
            ActivityInstanceId = item.ActivityInstanceId,
            ActivityTypeName = item.ActivityTypeName,
            Options = item.Options,
            CreatedAt = systemClock.UtcNow,
        };

        logger.LogDebug("Enqueuing bookmark queue item {BookmarkQueueItemId} with bookmark {BookmarkId} and stimulus {StimulusHash}", entity.Id, entity.BookmarkId, entity.StimulusHash);
        
        await store.AddAsync(entity, cancellationToken);

        // Trigger the bookmark queue processor.
        await bookmarkQueueSignaler.TriggerAsync(cancellationToken);
    }
}