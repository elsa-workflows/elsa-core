using Elsa.Common;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime;

public class StoreBookmarkQueue(
    IBookmarkQueueStore store, 
    IBookmarkResumer resumer, 
    IBookmarkQueueSignaler bookmarkQueueSignaler, 
    ISystemClock systemClock, 
    IIdentityGenerator identityGenerator,
    ILogger<StoreBookmarkQueue> logger) : IBookmarkQueue
{
    public async Task EnqueueAsync(NewBookmarkQueueItem item, CancellationToken cancellationToken = default)
    {
        var filter = new BookmarkFilter
        {
            BookmarkId = item.BookmarkId,
            Hash = item.StimulusHash,
            WorkflowInstanceId = item.WorkflowInstanceId,
            ActivityTypeName = item.ActivityTypeName
        };

        var result = await resumer.ResumeAsync(filter, item.Options, cancellationToken);

        if (result.Matched)
        {
            logger.LogDebug("Successfully resumed workflow instance {WorkflowInstance} using bookmark {BookmarkId} for activity type {ActivityType}", item.WorkflowInstanceId, item.BookmarkId, item.ActivityTypeName);
            return;
        }

        // There was no matching bookmark yet. Store the queue item for the system to pick up whenever the bookmark becomes present.
        logger.LogDebug("No bookmark with ID {BookmarkId} found for workflow {WorkflowInstance} for activity type {ActivityType}. Adding the request to the bookmark queue", item.BookmarkId, item.WorkflowInstanceId, item.ActivityTypeName);
        
        var entity = new BookmarkQueueItem
        {
            Id = identityGenerator.GenerateId(),
            WorkflowInstanceId = item.WorkflowInstanceId,
            BookmarkId = item.BookmarkId,
            StimulusHash = item.StimulusHash,
            ActivityInstanceId = item.ActivityInstanceId,
            ActivityTypeName = item.ActivityTypeName,
            Options = item.Options,
            CreatedAt = systemClock.UtcNow,
        };

        await store.AddAsync(entity, cancellationToken);

        // Trigger the bookmark queue processor.
        await bookmarkQueueSignaler.TriggerAsync(cancellationToken);
    }
}