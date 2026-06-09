using System.Collections;
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
    private const string CurrentTenantId = "tenant-a";
    private const string OtherTenantId = "tenant-b";
    private const string BookmarkHash = "http-endpoint:/colliding:get";
    private const string BookmarkPath = "/colliding";
    private const string BasePath = "/workflows";
    private const string WorkflowRequestPath = BasePath + BookmarkPath;

    [Fact]
    public async Task InvokeAsync_WithCollidingHttpEndpointBookmarks_UsesTenantScopedBookmarkLookup()
    {
        var bookmarkStore = new CapturingBookmarkStore(CurrentTenantId, CreateCollidingHttpEndpointBookmarks());
        var serviceProvider = CreateServiceProvider(bookmarkStore);
        var middleware = CreateMiddleware();
        var httpContext = CreateHttpContext(serviceProvider, BookmarkPath);

        await middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = null }),
            new EmptyHttpWorkflowLookupService());

        Assert.NotNull(bookmarkStore.LastFilter);
        var filter = bookmarkStore.LastFilter!;
        Assert.False(filter.TenantAgnostic);
    }

    [Fact]
    public async Task InvokeAsync_OutsideConfiguredBasePath_SkipsRouteMatchingAndCallsNext()
    {
        var bookmarkStore = new CapturingBookmarkStore(CurrentTenantId, []);
        var routeMatcher = new TrackingRouteMatcher();
        var serviceProvider = CreateServiceProvider(bookmarkStore, routeMatcher: routeMatcher);
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var httpContext = CreateHttpContext(serviceProvider, "/api/orders");

        await middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = BasePath }),
            new EmptyHttpWorkflowLookupService());

        Assert.True(nextCalled);
        Assert.Equal(0, routeMatcher.CallCount);
        Assert.Null(bookmarkStore.LastFilter);
    }

    [Fact]
    public async Task InvokeAsync_WithinConfiguredBasePath_UsesMatchedRouteWithoutBasePathForBookmarkHash()
    {
        var bookmarkStore = new CapturingBookmarkStore(CurrentTenantId, []);
        var routeMatcher = new TrackingRouteMatcher();
        var stimulusHasher = new CapturingStimulusHasher();
        var routeTable = new ListRouteTable([new(WorkflowRequestPath)]);
        var serviceProvider = CreateServiceProvider(bookmarkStore, routeMatcher, routeTable, stimulusHasher);
        var middleware = CreateMiddleware();
        var httpContext = CreateHttpContext(serviceProvider, WorkflowRequestPath);

        await middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = BasePath }),
            new EmptyHttpWorkflowLookupService());

        Assert.Equal(1, routeMatcher.CallCount);
        Assert.Equal(WorkflowRequestPath, routeMatcher.LastRouteTemplate);
        Assert.Equal(WorkflowRequestPath, routeMatcher.LastRoute);
        Assert.NotNull(stimulusHasher.LastPayload);
        Assert.Equal(BookmarkPath, stimulusHasher.LastPayload!.Path);
        Assert.Equal("get", stimulusHasher.LastPayload.Method);
        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
    }

    private static HttpWorkflowsMiddleware CreateMiddleware(RequestDelegate? next = null) => new(next ?? (_ => Task.CompletedTask));

    private static DefaultHttpContext CreateHttpContext(IServiceProvider serviceProvider, string path)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Path = path;
        httpContext.Request.Method = HttpMethod.Get.Method;
        return httpContext;
    }

    private static IServiceProvider CreateServiceProvider(
        IBookmarkStore bookmarkStore,
        IRouteMatcher? routeMatcher = null,
        IRouteTable? routeTable = null,
        IStimulusHasher? stimulusHasher = null)
    {
        routeMatcher ??= new ExactRouteMatcher();
        routeTable ??= new ListRouteTable([new(BookmarkPath)]);
        stimulusHasher ??= new CapturingStimulusHasher();

        return new ServiceCollection()
            .AddSingleton<IBookmarkStore>(bookmarkStore)
            .AddSingleton<IRouteMatcher>(routeMatcher)
            .AddSingleton<IRouteTable>(routeTable)
            .AddSingleton<IStimulusHasher>(stimulusHasher)
            .BuildServiceProvider();
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
        Payload = new HttpEndpointBookmarkPayload(BookmarkPath, "get")
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

    private class CapturingStimulusHasher : IStimulusHasher
    {
        public HttpEndpointBookmarkPayload? LastPayload { get; private set; }

        public string Hash(string stimulusName, object? payload = null, string? activityInstanceId = null)
        {
            LastPayload = payload as HttpEndpointBookmarkPayload;
            return BookmarkHash;
        }
    }

    private class ExactRouteMatcher : IRouteMatcher
    {
        public RouteValueDictionary? Match(string routeTemplate, string route) => routeTemplate == route ? new() : null;
    }

    private class TrackingRouteMatcher : IRouteMatcher
    {
        public int CallCount { get; private set; }
        public string? LastRouteTemplate { get; private set; }
        public string? LastRoute { get; private set; }

        public RouteValueDictionary? Match(string routeTemplate, string route)
        {
            CallCount++;
            LastRouteTemplate = routeTemplate;
            LastRoute = route;
            return routeTemplate == route ? new() : null;
        }
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
