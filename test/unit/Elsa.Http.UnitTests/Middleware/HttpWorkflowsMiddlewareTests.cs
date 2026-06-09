using System.Collections;
using System.Reflection;
using Elsa.Http.Bookmarks;
using Elsa.Http.Middleware;
using Elsa.Http.Options;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.UnitTests.Middleware;

public class HttpWorkflowsMiddlewareTests
{
    private static readonly MethodInfo ExecuteWithinTimeoutAsyncMethod = typeof(HttpWorkflowsMiddleware).GetMethod("ExecuteWithinTimeoutAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private const string CurrentTenantId = "tenant-a";
    private const string OtherTenantId = "tenant-b";
    private const string BookmarkHash = "http-endpoint:/colliding:get";

    private readonly CapturingBookmarkStore _bookmarkStore;
    private readonly IServiceProvider _serviceProvider;
    private readonly HttpWorkflowsMiddleware _middleware = new(_ => Task.CompletedTask);

    public HttpWorkflowsMiddlewareTests()
    {
        _bookmarkStore = new(CurrentTenantId, CreateCollidingHttpEndpointBookmarks());
        _serviceProvider = new ServiceCollection()
            .AddSingleton<IBookmarkStore>(_bookmarkStore)
            .AddSingleton<IRouteMatcher, ExactRouteMatcher>()
            .AddSingleton<IRouteTable>(new ListRouteTable([new(BookmarkPath)]))
            .AddSingleton<IStimulusHasher, FixedStimulusHasher>()
            .BuildServiceProvider();
    }

    [Fact]
    public async Task InvokeAsync_WithCollidingHttpEndpointBookmarks_UsesTenantScopedBookmarkLookup()
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        httpContext.Request.Path = BookmarkPath;
        httpContext.Request.Method = HttpMethod.Get.Method;

        await _middleware.InvokeAsync(
            httpContext,
            _serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = null }),
            new EmptyHttpWorkflowLookupService());

        Assert.NotNull(_bookmarkStore.LastFilter);
        var filter = _bookmarkStore.LastFilter!;
        Assert.False(filter.TenantAgnostic);
    }

    [Fact]
    public async Task ExecuteWithinTimeoutAsync_RestoresRequestAbortedAfterSuccess()
    {
        using var requestAbortedSource = new CancellationTokenSource();
        var httpContext = new DefaultHttpContext { RequestAborted = requestAbortedSource.Token };
        var observedToken = CancellationToken.None;

        var result = await ExecuteWithinTimeoutAsync(async cancellationToken =>
        {
            observedToken = cancellationToken;
            Assert.Equal(cancellationToken, httpContext.RequestAborted);
            await Task.CompletedTask;
            return 42;
        }, TimeSpan.FromSeconds(1), httpContext);

        Assert.Equal(42, result);
        Assert.NotEqual(requestAbortedSource.Token, observedToken);
        Assert.Equal(requestAbortedSource.Token, httpContext.RequestAborted);
    }

    [Fact]
    public async Task ExecuteWithinTimeoutAsync_RestoresRequestAbortedAfterFault()
    {
        using var requestAbortedSource = new CancellationTokenSource();
        var httpContext = new DefaultHttpContext { RequestAborted = requestAbortedSource.Token };

        await Assert.ThrowsAsync<InvalidOperationException>(() => ExecuteWithinTimeoutAsync<int>(_ => throw new InvalidOperationException("Boom"), TimeSpan.FromSeconds(1), httpContext));

        Assert.Equal(requestAbortedSource.Token, httpContext.RequestAborted);
    }

    [Fact]
    public async Task ExecuteWithinTimeoutAsync_RestoresRequestAbortedAfterCancellation()
    {
        using var requestAbortedSource = new CancellationTokenSource();
        requestAbortedSource.Cancel();

        var httpContext = new DefaultHttpContext { RequestAborted = requestAbortedSource.Token };
        var observedToken = CancellationToken.None;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ExecuteWithinTimeoutAsync<int>(cancellationToken =>
        {
            observedToken = cancellationToken;
            return Task.FromCanceled<int>(cancellationToken);
        }, TimeSpan.FromSeconds(1), httpContext));

        Assert.True(observedToken.IsCancellationRequested);
        Assert.Equal(requestAbortedSource.Token, httpContext.RequestAborted);
    }

    private async Task<T> ExecuteWithinTimeoutAsync<T>(Func<CancellationToken, Task<T>> action, TimeSpan? requestTimeout, HttpContext httpContext)
    {
        var method = ExecuteWithinTimeoutAsyncMethod.MakeGenericMethod(typeof(T));
        var task = (Task<T>)method.Invoke(_middleware, [action, requestTimeout, httpContext])!;
        return await task;
    }

    private static IEnumerable<StoredBookmark> CreateCollidingHttpEndpointBookmarks()
    {
        yield return CreateBookmark("current-tenant-bookmark", CurrentTenantId);
        yield return CreateBookmark("other-tenant-bookmark", OtherTenantId);
    }

    private const string BookmarkPath = "/colliding";

    private static StoredBookmark CreateBookmark(string id, string tenantId) => new()
    {
        Id = id,
        TenantId = tenantId,
        Hash = BookmarkHash,
        WorkflowInstanceId = $"{id}-workflow-instance",
        Payload = new HttpEndpointBookmarkPayload("/colliding", "get")
    };

    private class CapturingBookmarkStore(string currentTenantId, IEnumerable<StoredBookmark> bookmarks) : IBookmarkStore
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
            _ = Filter(filter).ToList();
            return new([]);
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

    private class EmptyHttpWorkflowLookupService : IHttpWorkflowLookupService
    {
        public Task<HttpWorkflowLookupResult?> FindWorkflowAsync(string bookmarkHash, CancellationToken cancellationToken = default) => Task.FromResult<HttpWorkflowLookupResult?>(null);
    }

    private class FixedStimulusHasher : IStimulusHasher
    {
        public string Hash(string stimulusName, object? payload = null, string? activityInstanceId = null) => BookmarkHash;
    }

    private class ExactRouteMatcher : IRouteMatcher
    {
        public RouteValueDictionary? Match(string routeTemplate, string route) => routeTemplate == route ? new() : null;
    }

    private class ListRouteTable(IEnumerable<HttpRouteData> routes) : IRouteTable
    {
        private readonly ICollection<HttpRouteData> _routes = routes.ToList();

        public IEnumerator<HttpRouteData> GetEnumerator() => _routes.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(string route) => _routes.Add(new(route));

        public void Add(HttpRouteData httpRouteData) => _routes.Add(httpRouteData);

        public void Remove(string route)
        {
            foreach (var routeData in _routes.Where(x => x.Route == route).ToList())
                _routes.Remove(routeData);
        }

        public void AddRange(IEnumerable<string> routes)
        {
            foreach (var route in routes)
                Add(route);
        }

        public void RemoveRange(IEnumerable<string> routes)
        {
            foreach (var route in routes)
                Remove(route);
        }
    }
}
