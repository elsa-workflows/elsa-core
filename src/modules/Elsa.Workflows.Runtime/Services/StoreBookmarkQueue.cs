using Elsa.Common.Contracts;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Workflows.Runtime;

public class StoreBookmarkQueue(IBookmarkQueueItemStore store, IBookmarkResumer resumer, ISystemClock systemClock, IIdentityGenerator identityGenerator) : IBookmarkQueue
{
    public async Task EnqueueAsync(NewBookmarkQueueItem item, CancellationToken cancellationToken = default)
    {
        var filter = new BookmarkFilter
        {
            BookmarkId = item.BookmarkId,
            Hash = item.BookmarkHash,
            WorkflowInstanceId = item.WorkflowInstanceId
        };

        var result = await resumer.ResumeAsync(filter, item.Options, cancellationToken);

        if (result.Matched)
            return;

        // There was no matching bookmark yet. Store the queue item for the system to pick up whenever the bookmark becomes present.
        var entity = new BookmarkQueueItem
        {
            Id = identityGenerator.GenerateId(),
            WorkflowInstanceId = item.WorkflowInstanceId,
            BookmarkId = item.BookmarkId,
            BookmarkHash = item.BookmarkHash,
            Options = item.Options,
            CreatedAt = systemClock.UtcNow,
        };

        await store.AddAsync(entity, cancellationToken);
    }
}