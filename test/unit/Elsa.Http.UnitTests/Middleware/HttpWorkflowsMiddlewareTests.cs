using System.Reflection;
using Elsa.Http.Bookmarks;
using Elsa.Http.Middleware;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.UnitTests.Middleware;

public class HttpWorkflowsMiddlewareTests
{
    private const string CurrentTenantId = "tenant-a";
    private const string OtherTenantId = "tenant-b";
    private const string BookmarkHash = "http-endpoint:/colliding:get";

    private readonly TenantAwareBookmarkStore _bookmarkStore;
    private readonly IServiceProvider _serviceProvider;
    private readonly HttpWorkflowsMiddleware _middleware = new(_ => Task.CompletedTask);

    public HttpWorkflowsMiddlewareTests()
    {
        _bookmarkStore = new(CurrentTenantId, CreateCollidingHttpEndpointBookmarks());
        _serviceProvider = new ServiceCollection()
            .AddSingleton<IBookmarkStore>(_bookmarkStore)
            .BuildServiceProvider();
    }

    [Fact]
    public async Task FindBookmarksAsync_WithCollidingHttpEndpointBookmarks_ReturnsOnlyCurrentTenantBookmark()
    {
        var bookmarks = await FindBookmarksAsync();

        var bookmark = Assert.Single(bookmarks);
        Assert.Equal(CurrentTenantId, bookmark.TenantId);
        Assert.False(_bookmarkStore.LastFilter?.TenantAgnostic);
    }

    private async Task<IEnumerable<StoredBookmark>> FindBookmarksAsync()
    {
        var method = typeof(HttpWorkflowsMiddleware).GetMethod("FindBookmarksAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var result = method.Invoke(_middleware, [_serviceProvider, BookmarkHash, null, null, CancellationToken.None]);
        return await (Task<IEnumerable<StoredBookmark>>)result!;
    }

    private static IEnumerable<StoredBookmark> CreateCollidingHttpEndpointBookmarks()
    {
        yield return CreateBookmark("current-tenant-bookmark", CurrentTenantId);
        yield return CreateBookmark("other-tenant-bookmark", OtherTenantId);
    }

    private static StoredBookmark CreateBookmark(string id, string tenantId) => new()
    {
        Id = id,
        TenantId = tenantId,
        Hash = BookmarkHash,
        WorkflowInstanceId = $"{id}-workflow-instance",
        Payload = new HttpEndpointBookmarkPayload("/colliding", "get")
    };

    private class TenantAwareBookmarkStore(string currentTenantId, IEnumerable<StoredBookmark> bookmarks) : IBookmarkStore
    {
        private readonly ICollection<StoredBookmark> _bookmarks = bookmarks.ToList();

        public BookmarkFilter? LastFilter { get; private set; }

        public ValueTask SaveAsync(StoredBookmark record, CancellationToken cancellationToken = default)
        {
            _bookmarks.Add(record);
            return ValueTask.CompletedTask;
        }

        public ValueTask SaveManyAsync(IEnumerable<StoredBookmark> records, CancellationToken cancellationToken)
        {
            foreach (var record in records)
                _bookmarks.Add(record);

            return ValueTask.CompletedTask;
        }

        public ValueTask<StoredBookmark?> FindAsync(BookmarkFilter filter, CancellationToken cancellationToken = default)
        {
            LastFilter = filter;
            return new(Filter(filter).FirstOrDefault());
        }

        public ValueTask<IEnumerable<StoredBookmark>> FindManyAsync(BookmarkFilter filter, CancellationToken cancellationToken = default)
        {
            LastFilter = filter;
            return new(Filter(filter).ToList());
        }

        public ValueTask<long> DeleteAsync(BookmarkFilter filter, CancellationToken cancellationToken = default)
        {
            var bookmarksToDelete = Filter(filter).ToList();

            foreach (var bookmark in bookmarksToDelete)
                _bookmarks.Remove(bookmark);

            return new(bookmarksToDelete.Count);
        }

        private IEnumerable<StoredBookmark> Filter(BookmarkFilter filter)
        {
            var query = filter.Apply(_bookmarks.AsQueryable());

            if (!filter.TenantAgnostic)
                query = query.Where(x => x.TenantId == currentTenantId);

            return query;
        }
    }
}
