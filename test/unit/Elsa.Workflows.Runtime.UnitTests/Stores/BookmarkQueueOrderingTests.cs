using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Elsa.Workflows.Runtime.Stores;

namespace Elsa.Workflows.Runtime.UnitTests.Stores;

public class BookmarkQueueOrderingTests
{
    [Theory]
    [InlineData(OrderDirection.Ascending)]
    [InlineData(OrderDirection.Descending)]
    public async Task OrdersTimestampTiesByIdAcrossPages(OrderDirection direction)
    {
        var store = new MemoryBookmarkQueueStore(new MemoryStore<BookmarkQueueItem>());
        var ids = Enumerable.Range(0, 55).Select(index => $"queue-{index:D2}").ToArray();
        foreach (var id in ids.Reverse())
            await store.AddAsync(new() { Id = id, WorkflowInstanceId = "selected", CreatedAt = DateTimeOffset.UnixEpoch });

        const string newest = "newest";
        await store.AddAsync(new() { Id = newest, WorkflowInstanceId = "selected", CreatedAt = DateTimeOffset.UnixEpoch.AddDays(1) });
        await store.AddAsync(new() { Id = "excluded", CreatedAt = DateTimeOffset.UnixEpoch.AddDays(2) });

        var order = new BookmarkQueueItemOrder<DateTimeOffset>(item => item.CreatedAt, direction);
        var filter = new BookmarkQueueFilter { WorkflowInstanceId = "selected" };
        var expected = direction == OrderDirection.Ascending ? ids.Append(newest) : ids.Prepend(newest);
        var firstPage = await store.PageAsync(PageArgs.FromRange(0, 50), filter, order);
        var secondPage = await store.PageAsync(PageArgs.FromRange(50, 50), filter, order);

        Assert.Equal(56, firstPage.TotalCount);
        Assert.Equal(expected, firstPage.Items.Concat(secondPage.Items).Select(item => item.Id));

        var unfilteredOffset = direction == OrderDirection.Ascending ? 0 : 2;
        var unfiltered = await store.PageAsync(PageArgs.FromRange(unfilteredOffset, 2), order);
        Assert.Equal(ids.Take(2), unfiltered.Items.Select(item => item.Id));
    }
}
