using Elsa.Common;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime;

[UsedImplicitly]
public class DefaultBookmarkQueuePurger(IBookmarkQueueStore store, ISystemClock systemClock, ILogger<DefaultBookmarkQueuePurger> logger) : IBookmarkQueuePurger
{
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(1);
    private readonly int _batchSize = 50;
    
    public async Task PurgeAsync(CancellationToken cancellationToken = default)
    {
        var currentPage = 0;
        var now = systemClock.UtcNow;
        
        logger.LogInformation("Purging bookmark queue items older than {Ttl}.", _ttl);

        while (true)
        {
            var pageArgs = PageArgs.FromPage(currentPage, _batchSize);
            var filter = new BookmarkQueueFilter {CreatedAtLessThan = now - _ttl};
            var order = new BookmarkQueueItemOrder<DateTimeOffset>(x => x.CreatedAt, OrderDirection.Ascending);
            var page = await store.PageAsync(pageArgs, filter, order, cancellationToken);
            var items = page.Items;
            
            if (items.Count == 0)
                break;
            
            var ids = items.Select(x => x.Id).ToList();
            await store.DeleteAsync(new BookmarkQueueFilter {Ids = ids}, cancellationToken);
            
            logger.LogInformation("Purged {Count} bookmark queue items.", items.Count);
            
            currentPage++;
        }
        
        logger.LogInformation("Finished purging bookmark queue items.");
    }
}