using System.Collections;
using Elsa.Common.Models;
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
using NSubstitute;

namespace Elsa.Http.UnitTests.Middleware;

public class HttpWorkflowsMiddlewareTests
{
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
    public async Task InvokeAsync_WithConfiguredBasePathAndNonMatchingPath_SkipsRouteMatchingAndCallsNext()
    {
        var nextCalled = false;
        var middleware = new HttpWorkflowsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Path = "/health";

        await middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = "/workflows" }),
            new EmptyHttpWorkflowLookupService());

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WithSiblingPrefixPath_SkipsRouteMatchingAndCallsNext()
    {
        var nextCalled = false;
        var middleware = new HttpWorkflowsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Path = "/workflows-v2/status";

        await middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = "/workflows" }),
            new EmptyHttpWorkflowLookupService());

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WithMultiplePrefixSegmentsBeforeBasePath_SkipsRouteMatchingAndCallsNext()
    {
        var nextCalled = false;
        var routeMatcher = Substitute.For<IRouteMatcher>();
        var middleware = new HttpWorkflowsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var serviceProvider = new ServiceCollection()
            .AddSingleton(routeMatcher)
            .AddSingleton<IRouteTable>(new ListRouteTable([new("/api/v1/workflows/status")]))
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Path = "/api/v1/workflows/status";

        await middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = "/workflows" }),
            new EmptyHttpWorkflowLookupService());

        Assert.True(nextCalled);
        routeMatcher.DidNotReceive().Match(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task InvokeAsync_WithNonTenantPrefixedBasePathSegment_CallsNext()
    {
        var nextCalled = false;
        var middleware = new HttpWorkflowsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IRouteMatcher, ExactRouteMatcher>()
            .AddSingleton<IRouteTable>(new ListRouteTable([]))
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Path = "/api/workflows/colliding";

        await middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = "/workflows" }),
            new EmptyHttpWorkflowLookupService());

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WithConfiguredBasePathAndMatchingPath_StillResolvesRoute()
    {
        var routeMatcher = Substitute.For<IRouteMatcher>();
        routeMatcher.Match("/workflows/colliding", "/workflows/colliding").Returns(new RouteValueDictionary());
        var bookmarkStore = new CapturingBookmarkStore(CurrentTenantId, CreateCollidingHttpEndpointBookmarks());
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IBookmarkStore>(bookmarkStore)
            .AddSingleton(routeMatcher)
            .AddSingleton<IRouteTable>(new ListRouteTable([new("/workflows/colliding")]))
            .AddSingleton<IStimulusHasher, FixedStimulusHasher>()
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Path = "/workflows/colliding";
        httpContext.Request.Method = HttpMethod.Get.Method;

        await _middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = "/workflows" }),
            new EmptyHttpWorkflowLookupService());

        routeMatcher.Received(1).Match("/workflows/colliding", "/workflows/colliding");
        Assert.NotNull(bookmarkStore.LastFilter);
    }

    [Fact]
    public async Task InvokeAsync_WithTenantPrefixedWorkflowPath_StillResolvesRoute()
    {
        var nextCalled = false;
        var routeMatcher = Substitute.For<IRouteMatcher>();
        var stimulusHasher = new CapturingStimulusHasher();
        var middleware = new HttpWorkflowsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        routeMatcher.Match("/{tenantPrefix}/workflows/colliding", "/acme/workflows/colliding").Returns(new RouteValueDictionary(new Dictionary<string, object?> { ["tenantPrefix"] = "acme" }));
        var bookmarkStore = new CapturingBookmarkStore(CurrentTenantId, CreateCollidingHttpEndpointBookmarks());
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IBookmarkStore>(bookmarkStore)
            .AddSingleton(routeMatcher)
            .AddSingleton<IRouteTable>(new ListRouteTable([new("/{tenantPrefix}/workflows/colliding")]))
            .AddSingleton<IStimulusHasher>(stimulusHasher)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Path = "/acme/workflows/colliding";
        httpContext.Request.Method = HttpMethod.Get.Method;

        await middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = "/workflows" }),
            new EmptyHttpWorkflowLookupService());

        Assert.False(nextCalled);
        routeMatcher.Received(1).Match("/{tenantPrefix}/workflows/colliding", "/acme/workflows/colliding");
        Assert.Equal("/colliding", stimulusHasher.LastPayload?.Path);
        Assert.NotNull(bookmarkStore.LastFilter);
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

        public ValueTask<Page<StoredBookmark>> FindManyAsync(BookmarkFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
        {
            LastFilter = filter;
            var results = Filter(filter).ToList();
            return new(Page.Of(results, results.Count));
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
