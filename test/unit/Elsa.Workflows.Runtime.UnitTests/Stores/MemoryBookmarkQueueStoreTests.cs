using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Common.Services;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Elsa.Workflows.Runtime.Stores;

namespace Elsa.Workflows.Runtime.UnitTests.Stores;

public class MemoryBookmarkQueueStoreTests
{
    /// <summary>
    /// Pages timestamp ties by ID while preserving the caller's primary direction and workflow filter.
    /// </summary>
    [Theory]
    [InlineData(false, OrderDirection.Ascending)]
    [InlineData(false, OrderDirection.Descending)]
    [InlineData(true, OrderDirection.Ascending)]
    [InlineData(true, OrderDirection.Descending)]
    public async Task PageAsync_BreaksTimestampTiesById(bool filtered, OrderDirection direction)
    {
        var store = new MemoryBookmarkQueueStore(new MemoryStore<BookmarkQueueItem>());
        for (var i = 119; i >= 0; i--)
        {
            await store.AddAsync(new BookmarkQueueItem
            {
                Id = $"item-{i:D3}",
                WorkflowInstanceId = i % 2 == 0 ? "selected" : "other",
                CreatedAt = DateTimeOffset.UnixEpoch.AddSeconds(i / 40)
            });
        }

        var candidates = Enumerable.Range(0, 120).Where(i => !filtered || i % 2 == 0);
        var ordered = direction == OrderDirection.Ascending
            ? candidates.OrderBy(i => i / 40).ThenBy(i => i)
            : candidates.OrderByDescending(i => i / 40).ThenBy(i => i);
        var expected = ordered.Select(i => $"item-{i:D3}").ToArray();
        var observed = new List<string>();

        for (var offset = 0; offset < expected.Length; offset += 50)
        {
            var pageArgs = PageArgs.FromRange(offset, 50);
            var order = new BookmarkQueueItemOrder<DateTimeOffset>(x => x.CreatedAt, direction);
            var page = filtered
                ? await store.PageAsync(pageArgs, new BookmarkQueueFilter { WorkflowInstanceId = "selected" }, order)
                : await store.PageAsync(pageArgs, order);

            Assert.Equal(expected.Length, page.TotalCount);
            observed.AddRange(page.Items.Select(x => x.Id));
        }

        Assert.Equal(expected, observed);
    }
}
