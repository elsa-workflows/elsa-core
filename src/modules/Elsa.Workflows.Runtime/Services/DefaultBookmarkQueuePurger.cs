using Elsa.Common;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.OrderDefinitions;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime;

[UsedImplicitly]
public class DefaultBookmarkQueuePurger(
    IBookmarkQueueStore store,
    IBookmarkQueueDeadLetterStore deadLetterStore,
    IBookmarkQueueDeadLetterManager deadLetterManager,
    ISystemClock systemClock,
    IOptions<BookmarkQueuePurgeOptions> options,
    ILogger<DefaultBookmarkQueuePurger> logger) : IBookmarkQueuePurger
{
    public async Task PurgeAsync(CancellationToken cancellationToken = default)
    {
        var now = systemClock.UtcNow;
        var thresholdDate = now - options.Value.Ttl;

        logger.LogDebug("Purging bookmark queue items older than {ThresholdDate}.", thresholdDate);

        while (true)
        {
            var pageArgs = PageArgs.FromPage(0, options.Value.BatchSize);
            var filter = new BookmarkQueueFilter
            {
                CreatedAtLessThan = thresholdDate
            };
            var order = new BookmarkQueueItemOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending);
            var page = await store.PageAsync(pageArgs, filter, order, cancellationToken);
            var items = page.Items;

            if (items.Count == 0)
                break;

            var movedCount = 0;
            foreach (var item in items)
            {
                await deadLetterManager.DeadLetterAsync(item, "Expired", cancellationToken: cancellationToken);

                var deletedCount = await store.DeleteAsync(new BookmarkQueueFilter
                {
                    Id = item.Id
                }, cancellationToken);

                if (deletedCount == 0)
                    continue;

                movedCount++;
            }

            logger.LogInformation("Moved {Count} expired bookmark queue items to the dead-letter store.", movedCount);
        }

        await PurgeDeadLettersAsync(now, cancellationToken);

        logger.LogDebug("Finished purging bookmark queue items.");
    }

    private async Task PurgeDeadLettersAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var thresholdDate = now - options.Value.DeadLetterTtl;

        logger.LogDebug("Purging bookmark queue dead-letter items older than {ThresholdDate}.", thresholdDate);

        while (true)
        {
            var pageArgs = PageArgs.FromPage(0, options.Value.BatchSize);
            var filter = new BookmarkQueueDeadLetterFilter
            {
                DeadLetteredAtLessThan = thresholdDate
            };
            var order = new BookmarkQueueDeadLetterItemOrder<DateTimeOffset>(x => x.DeadLetteredAt, OrderDirection.Ascending);
            var page = await deadLetterStore.PageAsync(pageArgs, filter, order, cancellationToken);
            var items = page.Items;

            if (items.Count == 0)
                break;

            await deadLetterStore.DeleteAsync(new BookmarkQueueDeadLetterFilter
            {
                Ids = items.Select(x => x.Id).ToList()
            }, cancellationToken);

            logger.LogInformation("Purged {Count} bookmark queue dead-letter items.", items.Count);
        }
    }
}
