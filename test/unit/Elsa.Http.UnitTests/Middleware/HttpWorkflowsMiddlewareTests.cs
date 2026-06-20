using System.Collections;
using System.Reflection;
using Elsa.Http.Bookmarks;
using Elsa.Http.Extensions;
using Elsa.Http.Middleware;
using Elsa.Http.Options;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Elsa.Workflows.State;

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
    public async Task InvokeAsync_WithNonElsaBasePath_SkipsRouteMatching()
    {
        var nextCalled = false;
        var middleware = new HttpWorkflowsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IRouteMatcher, ThrowingRouteMatcher>()
            .AddSingleton<IRouteTable>(new ListRouteTable([]))
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        httpContext.Request.Path = "/non-elsa";
        httpContext.Request.Method = HttpMethod.Get.Method;

        await middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = "/workflows" }),
            new EmptyHttpWorkflowLookupService());

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task ExecuteWithinTimeoutAsync_WhenActionThrows_RestoresRequestAborted()
    {
        var httpContext = new DefaultHttpContext();
        using var originalCancellationTokenSource = new CancellationTokenSource();
        httpContext.RequestAborted = originalCancellationTokenSource.Token;

        await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeExecuteWithinTimeoutAsync<object>(
            _ => throw new InvalidOperationException("Boom"),
            TimeSpan.FromSeconds(1),
            httpContext));

        Assert.Equal(originalCancellationTokenSource.Token, httpContext.RequestAborted);
    }

    [Fact]
    public async Task HandleWorkflowFaultAsync_WhenReloadReturnsNull_FallsBackToInMemoryWorkflowState()
    {
        var workflowState = new WorkflowState
        {
            Id = "workflow-instance-1",
            DefinitionId = "definition-1",
            DefinitionVersionId = "definition-version-1",
            Incidents = [new ActivityIncident()]
        };
        var workflowInstanceManager = Substitute.For<IWorkflowInstanceManager>();
        var faultHandler = Substitute.For<IHttpEndpointFaultHandler>();
        var serviceProvider = new ServiceCollection()
            .AddSingleton(workflowInstanceManager)
            .AddSingleton(faultHandler)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        var result = new RunWorkflowResult(null!, workflowState, new Workflow(), null, Journal.Empty);

        workflowInstanceManager.FindByIdAsync(workflowState.Id, Arg.Any<CancellationToken>()).Returns((WorkflowInstance?)null);

        var handled = await InvokeHandleWorkflowFaultAsync(serviceProvider, httpContext, result, CancellationToken.None);

        Assert.True(handled);
        await faultHandler.Received(1).HandleAsync(Arg.Is<HttpEndpointFaultContext>(x => ReferenceEquals(x.WorkflowState, workflowState)));
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

    private class ThrowingRouteMatcher : IRouteMatcher
    {
        public RouteValueDictionary? Match(string routeTemplate, string route) => throw new InvalidOperationException("Route matching should have been skipped.");
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

    private Task<T> InvokeExecuteWithinTimeoutAsync<T>(Func<CancellationToken, Task<T>> action, TimeSpan? requestTimeout, HttpContext httpContext)
    {
        var method = typeof(HttpWorkflowsMiddleware).GetMethod("ExecuteWithinTimeoutAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var genericMethod = method.MakeGenericMethod(typeof(T));
        return (Task<T>)genericMethod.Invoke(_middleware, [action, requestTimeout, httpContext])!;
    }

    private Task<bool> InvokeHandleWorkflowFaultAsync(IServiceProvider serviceProvider, HttpContext httpContext, RunWorkflowResult result, CancellationToken cancellationToken)
    {
        var method = typeof(HttpWorkflowsMiddleware).GetMethod("HandleWorkflowFaultAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (Task<bool>)method.Invoke(_middleware, [serviceProvider, httpContext, result, cancellationToken])!;
    }
}
