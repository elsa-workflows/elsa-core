using System.Collections;
using Elsa.Common.Models;
using Elsa.Http.Bookmarks;
using Elsa.Http.Contracts;
using Elsa.Http.Middleware;
using Elsa.Http.Options;
using Elsa.ServerAndStudio.Web;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
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
        var routeMatcher = Substitute.For<IRouteMatcher>();
        var middleware = new HttpWorkflowsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var serviceProvider = new ServiceCollection()
            .AddSingleton(routeMatcher)
            .AddSingleton<IRouteTable>(new ListRouteTable([new("/{tenantPrefix}/workflows/colliding")]))
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
        routeMatcher.DidNotReceive().Match(Arg.Any<string>(), Arg.Any<string>());
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
    public async Task InvokeAsync_WithResolvedTenantPath_StillResolvesRoute()
    {
        var nextCalled = false;
        var routeMatcher = Substitute.For<IRouteMatcher>();
        var stimulusHasher = new CapturingStimulusHasher();
        var middleware = new HttpWorkflowsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        routeMatcher.Match("/workflows/colliding", "/workflows/colliding").Returns(new RouteValueDictionary());
        var bookmarkStore = new CapturingBookmarkStore(CurrentTenantId, CreateCollidingHttpEndpointBookmarks());
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IBookmarkStore>(bookmarkStore)
            .AddSingleton(routeMatcher)
            .AddSingleton<IRouteTable>(new ListRouteTable([new("/workflows/colliding")]))
            .AddSingleton<IStimulusHasher>(stimulusHasher)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.PathBase = "/acme";
        httpContext.Request.Path = "/workflows/colliding";
        httpContext.Request.Method = HttpMethod.Get.Method;

        await middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = "/workflows" }),
            new EmptyHttpWorkflowLookupService());

        Assert.False(nextCalled);
        routeMatcher.Received(1).Match("/workflows/colliding", "/workflows/colliding");
        Assert.Equal("/colliding", stimulusHasher.LastPayload?.Path);
        Assert.NotNull(bookmarkStore.LastFilter);
    }

    [Fact]
    public async Task InvokeAsync_WhenATriggerMatches_StartsTheWorkflowWithTheTriggersHash()
    {
        const string triggerHash = "http-endpoint-trigger-hash";
        var workflow = new Workflow { Id = "workflow" };
        var root = new ActivityNode(workflow, "Root");
        var trigger = new StoredTrigger
        {
            Id = "trigger-1",
            WorkflowDefinitionVersionId = "workflow-version",
            ActivityId = "http-endpoint",
            Hash = triggerHash,
            Payload = new HttpEndpointBookmarkPayload(BookmarkPath, "get", authorize: false)
        };
        var workflowRunner = Substitute.For<IWorkflowRunner>();
        RunWorkflowOptions? runOptions = null;
        workflowRunner
            .RunAsync(Arg.Any<WorkflowGraph>(), Arg.Do<RunWorkflowOptions?>(options => runOptions = options), Arg.Any<CancellationToken>())
            // The middleware reads only the resulting state's incidents.
            .Returns(new RunWorkflowResult(null!, new WorkflowState(), workflow, null, Journal.Empty));
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IRouteMatcher, ExactRouteMatcher>()
            .AddSingleton<IRouteTable>(new ListRouteTable([new(BookmarkPath)]))
            .AddSingleton<IStimulusHasher, FixedStimulusHasher>()
            .AddSingleton(Substitute.For<IHttpEndpointAuthorizationHandler>())
            .AddSingleton(workflowRunner)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Path = BookmarkPath;
        httpContext.Request.Method = HttpMethod.Get.Method;

        await _middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = null }),
            new FixedHttpWorkflowLookupService(new(new WorkflowGraph(workflow, root, [root]), [trigger])));

        Assert.NotNull(runOptions);
        Assert.Equal(trigger.ActivityId, runOptions.TriggerActivityId);
        Assert.NotNull(runOptions.Properties);
        Assert.Equal(triggerHash, runOptions.Properties[TriggerWorkflowProperties.StimulusHash]);
    }

    [Fact]
    public async Task InvokeAsync_WithMatchingTriggerUnderInstancesPrefix_StartsWorkflowBeforeUnmatchedFallback()
    {
        const string triggerHash = "http-endpoint-instances-start";
        var nextCalled = false;
        var workflow = new Workflow { Id = "workflow" };
        var root = new ActivityNode(workflow, "Root");
        var trigger = new StoredTrigger
        {
            Id = "trigger-instances-start",
            WorkflowDefinitionVersionId = "workflow-version",
            ActivityId = "http-endpoint",
            Hash = triggerHash,
            Payload = new HttpEndpointBookmarkPayload("/instances/r2-start", "post", authorize: false)
        };
        var workflowRunner = Substitute.For<IWorkflowRunner>();
        RunWorkflowOptions? runOptions = null;
        workflowRunner
            .RunAsync(Arg.Any<WorkflowGraph>(), Arg.Do<RunWorkflowOptions?>(options => runOptions = options), Arg.Any<CancellationToken>())
            .Returns(new RunWorkflowResult(null!, new WorkflowState(), workflow, null, Journal.Empty));
        var middleware = new HttpWorkflowsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IHttpWorkflowUnmatchedHandler>(new AlwaysFallbackUnmatchedHandler())
            .AddSingleton<IRouteMatcher, ExactRouteMatcher>()
            .AddSingleton<IRouteTable>(new ListRouteTable([new("/workflows/instances/r2-start")]))
            .AddSingleton<IStimulusHasher, FixedStimulusHasher>()
            .AddSingleton(Substitute.For<IHttpEndpointAuthorizationHandler>())
            .AddSingleton(workflowRunner)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Path = "/workflows/instances/r2-start";
        httpContext.Request.Method = HttpMethod.Post.Method;

        await middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = "/workflows" }),
            new FixedHttpWorkflowLookupService(new(new WorkflowGraph(workflow, root, [root]), [trigger])));

        Assert.False(nextCalled);
        Assert.NotNull(runOptions);
        Assert.Equal(trigger.ActivityId, runOptions.TriggerActivityId);
    }

    [Theory]
    [InlineData("/workflows/definitions")]
    [InlineData("/workflows/definitions/abc/edit")]
    [InlineData("/workflows/instances")]
    [InlineData("/workflows/instances/abc/view")]
    public async Task InvokeAsync_WithUnmatchedStudioPageAndHostFallback_CallsNext(string path)
    {
        var nextCalled = false;
        var middleware = new HttpWorkflowsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IHttpWorkflowUnmatchedHandler>(new CombinedStudioUnmatchedHttpHandler())
            .AddSingleton<IBookmarkStore>(new CapturingBookmarkStore(CurrentTenantId, []))
            .AddSingleton<IRouteMatcher, ExactRouteMatcher>()
            .AddSingleton<IRouteTable>(new ListRouteTable([]))
            .AddSingleton<IStimulusHasher, FixedStimulusHasher>()
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Path = path;
        httpContext.Request.Method = HttpMethod.Get.Method;

        await middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = "/workflows" }),
            new EmptyHttpWorkflowLookupService());

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WithUnmatchedReservedPrefixHttpPath_DoesNotCallNext()
    {
        var nextCalled = false;
        var middleware = new HttpWorkflowsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IHttpWorkflowUnmatchedHandler>(new CombinedStudioUnmatchedHttpHandler())
            .AddSingleton<IBookmarkStore>(new CapturingBookmarkStore(CurrentTenantId, []))
            .AddSingleton<IRouteMatcher, ExactRouteMatcher>()
            .AddSingleton<IRouteTable>(new ListRouteTable([]))
            .AddSingleton<IStimulusHasher, FixedStimulusHasher>()
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Path = "/workflows/instances/r2-start";
        httpContext.Request.Method = HttpMethod.Post.Method;

        await middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = "/workflows" }),
            new EmptyHttpWorkflowLookupService());

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
    }

    [Theory]
    [InlineData("/workflows/definitions")]
    [InlineData("/workflows/definitions/")]
    [InlineData("/WORKFLOWS/DEFINITIONS")]
    [InlineData("/workflows/definitions/abc/edit")]
    [InlineData("/workflows/instances")]
    [InlineData("/workflows/instances/abc/view")]
    public async Task InvokeAsync_WhenPaused_UnmatchedStudioPage_CallsNext(string path)
    {
        var nextCalled = false;
        var workflowRunner = Substitute.For<IWorkflowRunner>();
        var middleware = new HttpWorkflowsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IHttpWorkflowUnmatchedHandler>(new CombinedStudioUnmatchedHttpHandler())
            .AddSingleton(CreatePausedQuiescenceSignal())
            .AddSingleton<IBookmarkStore>(new CapturingBookmarkStore(CurrentTenantId, []))
            .AddSingleton<IRouteMatcher, ExactRouteMatcher>()
            .AddSingleton<IRouteTable>(new ListRouteTable([]))
            .AddSingleton<IStimulusHasher, FixedStimulusHasher>()
            .AddSingleton(workflowRunner)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Path = path;
        httpContext.Request.QueryString = new QueryString("?version=1");
        httpContext.Request.Method = HttpMethod.Get.Method;

        await middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = "/workflows" }),
            new EmptyHttpWorkflowLookupService());

        Assert.True(nextCalled);
        Assert.NotEqual(StatusCodes.Status503ServiceUnavailable, httpContext.Response.StatusCode);
        await workflowRunner.DidNotReceive().RunAsync(Arg.Any<WorkflowGraph>(), Arg.Any<RunWorkflowOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_WhenPaused_MatchingTrigger_Returns503WithoutStarting()
    {
        const string triggerHash = "http-endpoint-paused-start";
        var nextCalled = false;
        var workflow = new Workflow { Id = "workflow" };
        var root = new ActivityNode(workflow, "Root");
        var trigger = new StoredTrigger
        {
            Id = "trigger-paused-start",
            WorkflowDefinitionVersionId = "workflow-version",
            ActivityId = "http-endpoint",
            Hash = triggerHash,
            Payload = new HttpEndpointBookmarkPayload("/instances/r2-start", "post", authorize: false)
        };
        var workflowRunner = Substitute.For<IWorkflowRunner>();
        var middleware = new HttpWorkflowsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IHttpWorkflowUnmatchedHandler>(new CombinedStudioUnmatchedHttpHandler())
            .AddSingleton(CreatePausedQuiescenceSignal())
            .AddSingleton<IRouteMatcher, ExactRouteMatcher>()
            .AddSingleton<IRouteTable>(new ListRouteTable([new("/workflows/instances/r2-start")]))
            .AddSingleton<IStimulusHasher, FixedStimulusHasher>()
            .AddSingleton(Substitute.For<IHttpEndpointAuthorizationHandler>())
            .AddSingleton(workflowRunner)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Path = "/workflows/instances/r2-start";
        httpContext.Request.Method = HttpMethod.Post.Method;

        await middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = "/workflows" }),
            new FixedHttpWorkflowLookupService(new(new WorkflowGraph(workflow, root, [root]), [trigger])));

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, httpContext.Response.StatusCode);
        Assert.Equal("60", httpContext.Response.Headers.RetryAfter.ToString());
        await workflowRunner.DidNotReceive().RunAsync(Arg.Any<WorkflowGraph>(), Arg.Any<RunWorkflowOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_WhenPaused_MatchingBookmark_Returns503WithoutResuming()
    {
        var nextCalled = false;
        var workflowRunner = Substitute.For<IWorkflowRunner>();
        var middleware = new HttpWorkflowsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var bookmark = CreateBookmark("paused-resume-bookmark", CurrentTenantId);
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IHttpWorkflowUnmatchedHandler>(new CombinedStudioUnmatchedHttpHandler())
            .AddSingleton(CreatePausedQuiescenceSignal())
            .AddSingleton<IBookmarkStore>(new CapturingBookmarkStore(CurrentTenantId, [bookmark], returnMatches: true))
            .AddSingleton<IRouteMatcher, ExactRouteMatcher>()
            .AddSingleton<IRouteTable>(new ListRouteTable([new("/workflows/instances/r2-start")]))
            .AddSingleton<IStimulusHasher, FixedStimulusHasher>()
            .AddSingleton(Substitute.For<IHttpEndpointAuthorizationHandler>())
            .AddSingleton(workflowRunner)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Path = "/workflows/instances/r2-start";
        httpContext.Request.Method = HttpMethod.Get.Method;

        await middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = "/workflows" }),
            new EmptyHttpWorkflowLookupService());

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, httpContext.Response.StatusCode);
        await workflowRunner.DidNotReceive().RunAsync(Arg.Any<WorkflowGraph>(), Arg.Any<RunWorkflowOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_WhenPaused_UnmatchedNonPagePath_Returns404()
    {
        var nextCalled = false;
        var middleware = new HttpWorkflowsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IHttpWorkflowUnmatchedHandler>(new CombinedStudioUnmatchedHttpHandler())
            .AddSingleton(CreatePausedQuiescenceSignal())
            .AddSingleton<IBookmarkStore>(new CapturingBookmarkStore(CurrentTenantId, []))
            .AddSingleton<IRouteMatcher, ExactRouteMatcher>()
            .AddSingleton<IRouteTable>(new ListRouteTable([]))
            .AddSingleton<IStimulusHasher, FixedStimulusHasher>()
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Path = "/workflows/qa-hello";
        httpContext.Request.Method = HttpMethod.Get.Method;

        await middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = "/workflows" }),
            new EmptyHttpWorkflowLookupService());

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenPaused_ExactStudioPageCollision_Returns503WithoutStudioFallback()
    {
        const string triggerHash = "http-endpoint-paused-collision";
        var nextCalled = false;
        var workflow = new Workflow { Id = "workflow" };
        var root = new ActivityNode(workflow, "Root");
        var trigger = new StoredTrigger
        {
            Id = "trigger-paused-collision",
            WorkflowDefinitionVersionId = "workflow-version",
            ActivityId = "http-endpoint",
            Hash = triggerHash,
            Payload = new HttpEndpointBookmarkPayload("/instances", "get", authorize: false)
        };
        var workflowRunner = Substitute.For<IWorkflowRunner>();
        var middleware = new HttpWorkflowsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IHttpWorkflowUnmatchedHandler>(new CombinedStudioUnmatchedHttpHandler())
            .AddSingleton(CreatePausedQuiescenceSignal())
            .AddSingleton<IRouteMatcher, ExactRouteMatcher>()
            .AddSingleton<IRouteTable>(new ListRouteTable([new("/workflows/instances")]))
            .AddSingleton<IStimulusHasher, FixedStimulusHasher>()
            .AddSingleton(Substitute.For<IHttpEndpointAuthorizationHandler>())
            .AddSingleton(workflowRunner)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Path = "/workflows/instances";
        httpContext.Request.Method = HttpMethod.Get.Method;

        await middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = "/workflows" }),
            new FixedHttpWorkflowLookupService(new(new WorkflowGraph(workflow, root, [root]), [trigger])));

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, httpContext.Response.StatusCode);
        await workflowRunner.DidNotReceive().RunAsync(Arg.Any<WorkflowGraph>(), Arg.Any<RunWorkflowOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_WhenAcceptingWork_UnmatchedStudioPage_StillCallsNext()
    {
        var nextCalled = false;
        var middleware = new HttpWorkflowsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var signal = Substitute.For<IQuiescenceSignal>();
        signal.IsAcceptingNewWork.Returns(true);
        signal.CurrentState.Returns(QuiescenceState.Initial("generation"));
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IHttpWorkflowUnmatchedHandler>(new CombinedStudioUnmatchedHttpHandler())
            .AddSingleton(signal)
            .AddSingleton<IBookmarkStore>(new CapturingBookmarkStore(CurrentTenantId, []))
            .AddSingleton<IRouteMatcher, ExactRouteMatcher>()
            .AddSingleton<IRouteTable>(new ListRouteTable([]))
            .AddSingleton<IStimulusHasher, FixedStimulusHasher>()
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Path = "/workflows/definitions";
        httpContext.Request.Method = HttpMethod.Get.Method;

        await middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = "/workflows" }),
            new EmptyHttpWorkflowLookupService());

        Assert.True(nextCalled);
    }

    [Theory]
    [InlineData("/workflows/definitions", true)]
    [InlineData("/workflows/definitions/", true)]
    [InlineData("/WORKFLOWS/Instances", true)]
    [InlineData("/workflows/definitions/abc/edit", true)]
    [InlineData("/workflows/instances/abc/view", true)]
    [InlineData("/workflows/instances/r2-start", false)]
    [InlineData("/workflows/definitions/qa-start", false)]
    [InlineData("/workflows/qa-hello", false)]
    [InlineData("/login", false)]
    public void CombinedStudioUnmatchedHttpHandler_MatchesExactStudioPagesOnly(string path, bool expected)
    {
        Assert.Equal(expected, CombinedStudioUnmatchedHttpHandler.IsStudioWorkflowPage(path));
    }

    [Fact]
    public async Task InvokeAsync_WithUnmatchedHttpTriggerPath_DoesNotCallNext()
    {
        var nextCalled = false;
        var middleware = new HttpWorkflowsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IBookmarkStore>(new CapturingBookmarkStore(CurrentTenantId, []))
            .AddSingleton<IRouteMatcher, ExactRouteMatcher>()
            .AddSingleton<IRouteTable>(new ListRouteTable([]))
            .AddSingleton<IStimulusHasher, FixedStimulusHasher>()
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Path = "/workflows/qa-hello";
        httpContext.Request.Method = HttpMethod.Get.Method;

        await middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = "/workflows" }),
            new EmptyHttpWorkflowLookupService());

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
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

    private static IQuiescenceSignal CreatePausedQuiescenceSignal()
    {
        var signal = Substitute.For<IQuiescenceSignal>();
        var state = new QuiescenceState(QuiescenceReason.AdministrativePause, DateTimeOffset.UtcNow, null, "test", "test", "generation");
        signal.IsAcceptingNewWork.Returns(false);
        signal.CurrentState.Returns(state);
        return signal;
    }

    private class CapturingBookmarkStore(string currentTenantId, IEnumerable<StoredBookmark> bookmarks, bool returnMatches = false) : IBookmarkStore
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
            var results = Filter(filter).ToList();
            return new(returnMatches ? results : []);
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

    private sealed class AlwaysFallbackUnmatchedHandler : IHttpWorkflowUnmatchedHandler
    {
        public async Task<bool> HandleAsync(HttpContext httpContext, RequestDelegate next, CancellationToken cancellationToken)
        {
            await next(httpContext);
            return true;
        }
    }

    private class EmptyHttpWorkflowLookupService : IHttpWorkflowLookupService
    {
        public Task<HttpWorkflowLookupResult?> FindWorkflowAsync(string bookmarkHash, CancellationToken cancellationToken = default) => Task.FromResult<HttpWorkflowLookupResult?>(null);
    }

    private class FixedHttpWorkflowLookupService(HttpWorkflowLookupResult result) : IHttpWorkflowLookupService
    {
        public Task<HttpWorkflowLookupResult?> FindWorkflowAsync(string bookmarkHash, CancellationToken cancellationToken = default) => Task.FromResult<HttpWorkflowLookupResult?>(result);
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
