using System.Collections;
using System.Reflection;
using Elsa.Http.Bookmarks;
using Elsa.Http.Middleware;
using Elsa.Http.Options;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.State;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Http.UnitTests.Middleware;

public class HttpWorkflowsMiddlewareTests
{
    private static readonly MethodInfo HandleWorkflowFaultAsyncMethod = typeof(HttpWorkflowsMiddleware).GetMethod("HandleWorkflowFaultAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
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
    public async Task HandleWorkflowFaultAsync_UsesReloadedWorkflowState_WhenAvailable()
    {
        var workflowState = CreateFaultedWorkflowState("workflow-1");
        var reloadedWorkflowState = CreateFaultedWorkflowState(workflowState.Id);
        var workflowInstanceManager = Substitute.For<IWorkflowInstanceManager>();
        var httpEndpointFaultHandler = Substitute.For<IHttpEndpointFaultHandler>();
        var serviceProvider = new ServiceCollection()
            .AddSingleton(workflowInstanceManager)
            .AddSingleton(httpEndpointFaultHandler)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext();
        var workflowInstance = new WorkflowInstance
        {
            Id = workflowState.Id,
            DefinitionId = workflowState.DefinitionId,
            DefinitionVersionId = workflowState.DefinitionVersionId,
            WorkflowState = reloadedWorkflowState
        };

        workflowInstanceManager.FindByIdAsync(workflowState.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<WorkflowInstance?>(workflowInstance));

        var handled = await HandleWorkflowFaultAsync(serviceProvider, httpContext, CreateRunWorkflowResult(workflowState), CancellationToken.None);

        Assert.True(handled);
        await httpEndpointFaultHandler.Received(1).HandleAsync(Arg.Is<HttpEndpointFaultContext>(context => ReferenceEquals(context.WorkflowState, reloadedWorkflowState)));
    }

    [Fact]
    public async Task HandleWorkflowFaultAsync_FallsBackToExecutionResultState_WhenReloadReturnsNull()
    {
        var workflowState = CreateFaultedWorkflowState("workflow-2");
        var workflowInstanceManager = Substitute.For<IWorkflowInstanceManager>();
        var httpEndpointFaultHandler = Substitute.For<IHttpEndpointFaultHandler>();
        var serviceProvider = new ServiceCollection()
            .AddSingleton(workflowInstanceManager)
            .AddSingleton(httpEndpointFaultHandler)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext();

        workflowInstanceManager.FindByIdAsync(workflowState.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<WorkflowInstance?>(null));

        var handled = await HandleWorkflowFaultAsync(serviceProvider, httpContext, CreateRunWorkflowResult(workflowState), CancellationToken.None);

        Assert.True(handled);
        await httpEndpointFaultHandler.Received(1).HandleAsync(Arg.Is<HttpEndpointFaultContext>(context => ReferenceEquals(context.WorkflowState, workflowState)));
    }

    private async Task<bool> HandleWorkflowFaultAsync(IServiceProvider serviceProvider, HttpContext httpContext, RunWorkflowResult workflowExecutionResult, CancellationToken cancellationToken)
    {
        var task = (Task<bool>)HandleWorkflowFaultAsyncMethod.Invoke(_middleware, [serviceProvider, httpContext, workflowExecutionResult, cancellationToken])!;
        return await task;
    }

    private static RunWorkflowResult CreateRunWorkflowResult(WorkflowState workflowState) => new(default!, workflowState, default!, null, Journal.Empty);

    private static WorkflowState CreateFaultedWorkflowState(string id) => new()
    {
        Id = id,
        DefinitionId = "definition",
        DefinitionVersionId = "definition-version",
        Incidents = new List<ActivityIncident>
        {
            new("activity", "activity-node", "TestActivity", "Boom", null, DateTimeOffset.UtcNow)
        }
    };

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