using System.Collections;
using Elsa.Common.DistributedHosting;
using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Http.Bookmarks;
using Elsa.Http.Contracts;
using Elsa.Http.Middleware;
using Elsa.Http.Options;
using Elsa.ServerAndStudio.Web;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Stores;
using Elsa.Workflows.State;
using Medallion.Threading;
using Medallion.Threading.FileSystem;
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
    public async Task InvokeAsync_WhenOnlyAnotherTenantHasAMatchingBookmark_ReturnsNotFound()
    {
        var tenantAccessor = new DefaultTenantAccessor();
        using var tenantScope = tenantAccessor.PushContext(new Tenant { Id = CurrentTenantId, Name = CurrentTenantId });
        var bookmarkStore = new MemoryBookmarkStore(new MemoryStore<StoredBookmark>(), tenantAccessor);
        await bookmarkStore.SaveAsync(CreateBookmark("other-tenant-bookmark", OtherTenantId));

        var workflowRunner = Substitute.For<IWorkflowRunner>();
        await using var serviceProvider = new ServiceCollection()
            .AddSingleton<IBookmarkStore>(bookmarkStore)
            .AddSingleton<IRouteMatcher, ExactRouteMatcher>()
            .AddSingleton<IRouteTable>(new ListRouteTable([new(BookmarkPath)]))
            .AddSingleton<IStimulusHasher, FixedStimulusHasher>()
            .AddSingleton(workflowRunner)
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        httpContext.Request.Path = "/workflows" + BookmarkPath;
        httpContext.Request.Method = HttpMethod.Get.Method;

        await _middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = new PathString("/workflows") }),
            new EmptyHttpWorkflowLookupService());

        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
        Assert.Empty(workflowRunner.ReceivedCalls());
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
            .AddSingleton<IWorkflowActivationGate, AllowingActivationGate>()
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
    public async Task InvokeAsync_WhenTheBookmarkedInstanceHasFinished_ReturnsNotFoundWithoutResuming()
    {
        // Arrange
        var resume = new ResumeScenario();
        resume.Instance.Status = WorkflowStatus.Finished;

        // Act
        var httpContext = await resume.InvokeAsync(_middleware);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
        Assert.Empty(resume.WorkflowRunner.ReceivedCalls());
    }

    [Fact]
    public async Task InvokeAsync_WhenTheInstanceFinishesWhileWaitingForTheInstanceLock_ReturnsNotFoundWithoutResuming()
    {
        // Arrange: another resume finishes the instance while this request waits for the lock.
        var resume = new ResumeScenario();
        resume.OnLockAcquired = () => resume.Instance.Status = WorkflowStatus.Finished;

        // Act
        var httpContext = await resume.InvokeAsync(_middleware);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
        Assert.Empty(resume.WorkflowRunner.ReceivedCalls());
        resume.LockProvider.Received(1).CreateLock($"workflow-instance:{resume.Instance.Id}");
    }

    [Fact]
    public async Task InvokeAsync_WhenTheBookmarkIsConsumedWhileWaitingForTheInstanceLock_ReturnsNotFoundWithoutResuming()
    {
        // Arrange: another resume consumes the bookmark while this request waits for the lock.
        var resume = new ResumeScenario();
        resume.OnLockAcquired = () => resume.Instance.WorkflowState.Bookmarks.Clear();

        // Act
        var httpContext = await resume.InvokeAsync(_middleware);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
        Assert.Empty(resume.WorkflowRunner.ReceivedCalls());
    }

    [Fact]
    public async Task InvokeAsync_WhenResumeAuthorizationIsDenied_DoesNotAcquireTheInstanceLock()
    {
        var resume = new ResumeScenario(authorize: true);
        resume.LockUnavailable = true;

        var httpContext = await resume.InvokeAsync(_middleware);

        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
        Assert.Empty(resume.LockProvider.ReceivedCalls());
        Assert.Empty(resume.WorkflowRunner.ReceivedCalls());
    }

    [Fact]
    public async Task InvokeAsync_WhenTheInstanceVersionChangesWhileWaitingForTheLock_DoesNotResumeIt()
    {
        var resume = new ResumeScenario();
        resume.OnLockAcquired = () => resume.Instance.DefinitionVersionId = "another-version";

        var httpContext = await resume.InvokeAsync(_middleware);

        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
        Assert.Empty(resume.WorkflowRunner.ReceivedCalls());
    }

    [Fact]
    public async Task InvokeAsync_WhenTwoRequestsMatchTheSameBookmark_ResumesItOnce()
    {
        var resume = new ResumeScenario();
        var lockDirectory = Directory.CreateTempSubdirectory();
        try
        {
            var lockName = $"workflow-instance:{resume.Instance.Id}";
            IDistributedLock fileLock = new FileDistributedSynchronizationProvider(lockDirectory).CreateLock(lockName);
            var instanceLock = Substitute.For<IDistributedLock>();
            var secondRequestReachedLock = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var firstRunStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var finishFirstRun = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var timeout = TimeSpan.FromSeconds(10);
            var lockAttempts = 0;
            var runs = 0;
            instanceLock.TryAcquireAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(call =>
            {
                if (Interlocked.Increment(ref lockAttempts) == 2)
                    secondRequestReachedLock.SetResult();

                return fileLock.TryAcquireAsync(call.Arg<TimeSpan>(), call.Arg<CancellationToken>());
            });
            resume.LockProvider.CreateLock(lockName).Returns(instanceLock);
            resume.WorkflowRunner
                .RunAsync(Arg.Any<Workflow>(), Arg.Any<WorkflowState>(), Arg.Any<RunWorkflowOptions?>(), Arg.Any<CancellationToken>())
                .Returns(async call =>
                {
                    if (Interlocked.Increment(ref runs) == 1)
                    {
                        firstRunStarted.SetResult();
                        await finishFirstRun.Task.WaitAsync(timeout);
                        resume.Instance.WorkflowState.Bookmarks.Clear();
                    }

                    return new RunWorkflowResult(null!, new WorkflowState(), call.Arg<Workflow>(), null, Journal.Empty);
                });

            var firstRequest = resume.InvokeAsync(_middleware);
            try
            {
                await firstRunStarted.Task.WaitAsync(timeout);
                var secondRequest = resume.InvokeAsync(_middleware);
                await Task.WhenAny(secondRequest, secondRequestReachedLock.Task).WaitAsync(timeout);
                finishFirstRun.TrySetResult();
                var secondResponse = await secondRequest.WaitAsync(timeout);

                Assert.Equal(1, runs);
                Assert.Equal(StatusCodes.Status404NotFound, secondResponse.Response.StatusCode);
            }
            finally
            {
                finishFirstRun.TrySetResult();
                await firstRequest.WaitAsync(timeout);
            }
        }
        finally
        {
            lockDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task InvokeAsync_WhenTheInstanceLockIsNotAcquiredInTime_ReturnsConflictWithoutResuming()
    {
        // Arrange: another run holds the instance lock for longer than this request waits.
        var resume = new ResumeScenario(requestTimeout: TimeSpan.FromSeconds(5));
        resume.LockUnavailable = true;

        // Act
        var httpContext = await resume.InvokeAsync(_middleware);

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        Assert.Empty(resume.WorkflowRunner.ReceivedCalls());
    }

    [Theory]
    [InlineData(600_000)]
    [InlineData(-1)] // Timeout.InfiniteTimeSpan
    public async Task InvokeAsync_WhenTheRequestTimeoutIsShorterThanTheLockTimeout_WaitsForTheInstanceLockNoLongerThanTheRequestTimeout(int lockTimeoutMilliseconds)
    {
        // Arrange
        var resume = new ResumeScenario(requestTimeout: TimeSpan.FromSeconds(5), lockTimeout: TimeSpan.FromMilliseconds(lockTimeoutMilliseconds));

        // Act
        await resume.InvokeAsync(_middleware);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(5), resume.RequestedLockTimeout);
    }

    [Fact]
    public async Task InvokeAsync_WhenThereIsNoRequestTimeout_WaitsForTheInstanceLockForTheConfiguredLockTimeout()
    {
        // Arrange
        var resume = new ResumeScenario();

        // Act
        await resume.InvokeAsync(_middleware);

        // Assert
        Assert.Equal(new DistributedLockingOptions().LockAcquisitionTimeout, resume.RequestedLockTimeout);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)] // Timeout.InfiniteTimeSpan
    [InlineData(60 * 60 * 1000)] // Longer than the default lock timeout.
    public async Task InvokeAsync_WhenTheRequestTimeoutIsNotAShorterPositiveValue_WaitsForTheInstanceLockForTheConfiguredLockTimeout(int requestTimeoutMilliseconds)
    {
        // Arrange
        var resume = new ResumeScenario(requestTimeout: TimeSpan.FromMilliseconds(requestTimeoutMilliseconds));

        // Act
        await resume.InvokeAsync(_middleware);

        // Assert
        Assert.Equal(new DistributedLockingOptions().LockAcquisitionTimeout, resume.RequestedLockTimeout);
    }

    [Fact]
    public async Task InvokeAsync_WhenTheBookmarkedInstanceIsRunning_ResumesItWhileHoldingTheInstanceLock()
    {
        // Arrange
        var resume = new ResumeScenario();
        var lockHeldDuringRun = false;
        resume.WorkflowRunner
            .When(x => x.RunAsync(Arg.Any<Workflow>(), Arg.Any<WorkflowState>(), Arg.Any<RunWorkflowOptions?>(), Arg.Any<CancellationToken>()))
            .Do(_ => lockHeldDuringRun = resume.LockHeld);

        // Act
        await resume.InvokeAsync(_middleware);

        // Assert
        await resume.WorkflowRunner.Received(1).RunAsync(Arg.Any<Workflow>(), resume.Instance.WorkflowState, Arg.Is<RunWorkflowOptions?>(x => x!.BookmarkId == ResumeScenario.BookmarkId), Arg.Any<CancellationToken>());
        Assert.True(lockHeldDuringRun);
        Assert.False(resume.LockHeld);
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
            .AddSingleton<IWorkflowActivationGate, AllowingActivationGate>()
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

    [Fact]
    public async Task InvokeAsync_WhenPaused_WithoutUnmatchedHandler_Returns503BeforeLookup()
    {
        var nextCalled = false;
        var workflowRunner = Substitute.For<IWorkflowRunner>();
        var workflowDefinitionService = Substitute.For<IWorkflowDefinitionService>();
        var lookup = new RecordingHttpWorkflowLookupService();
        var bookmarkStore = new CapturingBookmarkStore(CurrentTenantId, CreateCollidingHttpEndpointBookmarks());
        var middleware = new HttpWorkflowsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var serviceProvider = new ServiceCollection()
            .AddSingleton(CreatePausedQuiescenceSignal())
            .AddSingleton<IBookmarkStore>(bookmarkStore)
            .AddSingleton(workflowDefinitionService)
            .AddSingleton<IRouteMatcher, ExactRouteMatcher>()
            .AddSingleton<IRouteTable>(new ListRouteTable([new("/workflows/qa-hello")]))
            .AddSingleton<IStimulusHasher, FixedStimulusHasher>()
            .AddSingleton(workflowRunner)
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
            lookup);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, httpContext.Response.StatusCode);
        Assert.Equal("60", httpContext.Response.Headers.RetryAfter.ToString());
        Assert.Equal(0, lookup.Calls);
        Assert.Null(bookmarkStore.LastFilter);
        Assert.Empty(workflowDefinitionService.ReceivedCalls());
        await workflowRunner.DidNotReceive().RunAsync(Arg.Any<WorkflowGraph>(), Arg.Any<RunWorkflowOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_WhenDraining_WithoutUnmatchedHandler_Returns503BeforeLookup()
    {
        var nextCalled = false;
        var workflowRunner = Substitute.For<IWorkflowRunner>();
        var workflowDefinitionService = Substitute.For<IWorkflowDefinitionService>();
        var lookup = new RecordingHttpWorkflowLookupService();
        var bookmarkStore = new CapturingBookmarkStore(CurrentTenantId, CreateCollidingHttpEndpointBookmarks());
        var middleware = new HttpWorkflowsMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var serviceProvider = new ServiceCollection()
            .AddSingleton(CreateDrainingQuiescenceSignal())
            .AddSingleton<IBookmarkStore>(bookmarkStore)
            .AddSingleton(workflowDefinitionService)
            .AddSingleton<IRouteMatcher, ExactRouteMatcher>()
            .AddSingleton<IRouteTable>(new ListRouteTable([new("/workflows/qa-hello")]))
            .AddSingleton<IStimulusHasher, FixedStimulusHasher>()
            .AddSingleton(workflowRunner)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Path = "/workflows/qa-hello";
        httpContext.Request.Method = HttpMethod.Post.Method;

        await middleware.InvokeAsync(
            httpContext,
            serviceProvider,
            Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = "/workflows" }),
            lookup);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, httpContext.Response.StatusCode);
        Assert.Equal("5", httpContext.Response.Headers.RetryAfter.ToString());
        Assert.Equal(0, lookup.Calls);
        Assert.Null(bookmarkStore.LastFilter);
        Assert.Empty(workflowDefinitionService.ReceivedCalls());
        await workflowRunner.DidNotReceive().RunAsync(Arg.Any<WorkflowGraph>(), Arg.Any<RunWorkflowOptions?>(), Arg.Any<CancellationToken>());
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

    private static IQuiescenceSignal CreateDrainingQuiescenceSignal()
    {
        var signal = Substitute.For<IQuiescenceSignal>();
        var state = new QuiescenceState(QuiescenceReason.Drain, null, DateTimeOffset.UtcNow, null, null, "generation");
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

    private sealed class AllowingActivationGate : IWorkflowActivationGate
    {
        public Task<WorkflowActivationLease> EvaluateAsync(Workflow workflow, string? correlationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorkflowActivationLease(true, null));
    }

    private sealed class AlwaysFallbackUnmatchedHandler : IHttpWorkflowUnmatchedHandler
    {
        public async Task<bool> HandleAsync(HttpContext httpContext, RequestDelegate next, CancellationToken cancellationToken)
        {
            await next(httpContext);
            return true;
        }
    }

    /// <summary>
    /// A request that matches one HTTP bookmark of a running workflow instance, with a fake per-instance lock.
    /// </summary>
    private class ResumeScenario
    {
        public const string BookmarkId = "bookmark-1";
        private readonly StoredBookmark _bookmark;
        private readonly IServiceProvider _serviceProvider;

        public ResumeScenario(TimeSpan? requestTimeout = null, bool authorize = false, TimeSpan? lockTimeout = null)
        {
            _bookmark = new()
            {
                Id = BookmarkId,
                Hash = BookmarkHash,
                WorkflowInstanceId = "instance-1",
                Payload = new HttpEndpointBookmarkPayload(BookmarkPath, "get", authorize: authorize, requestTimeout: requestTimeout)
            };
            Instance = new()
            {
                Id = _bookmark.WorkflowInstanceId,
                DefinitionVersionId = "workflow-version",
                Status = WorkflowStatus.Running,
                WorkflowState = new() { Bookmarks = [new() { Id = BookmarkId }] }
            };

            var workflow = new Workflow { Id = "workflow" };
            var root = new ActivityNode(workflow, "Root");
            var bookmarkStore = Substitute.For<IBookmarkStore>();
            bookmarkStore.FindManyAsync(Arg.Any<BookmarkFilter>(), Arg.Any<CancellationToken>()).Returns(new ValueTask<IEnumerable<StoredBookmark>>([_bookmark]));
            var workflowInstanceStore = Substitute.For<IWorkflowInstanceStore>();
            workflowInstanceStore.FindAsync(Arg.Is<WorkflowInstanceFilter>(x => x.Id == Instance.Id), Arg.Any<CancellationToken>()).Returns(_ => new ValueTask<WorkflowInstance?>(Instance));
            var workflowDefinitionService = Substitute.For<IWorkflowDefinitionService>();
            workflowDefinitionService.FindWorkflowGraphAsync(Instance.DefinitionVersionId, Arg.Any<CancellationToken>()).Returns(new WorkflowGraph(workflow, root, [root]));
            WorkflowRunner
                .RunAsync(Arg.Any<Workflow>(), Arg.Any<WorkflowState>(), Arg.Any<RunWorkflowOptions?>(), Arg.Any<CancellationToken>())
                .Returns(new RunWorkflowResult(null!, new WorkflowState(), workflow, null, Journal.Empty));

            var lockHandle = Substitute.For<IDistributedSynchronizationHandle>();
            lockHandle.DisposeAsync().Returns(_ =>
            {
                LockHeld = false;
                return ValueTask.CompletedTask;
            });
            var instanceLock = Substitute.For<IDistributedLock>();
            instanceLock.TryAcquireAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(_ =>
            {
                RequestedLockTimeout = _.Arg<TimeSpan>();
                if (LockUnavailable)
                    return new ValueTask<IDistributedSynchronizationHandle?>((IDistributedSynchronizationHandle?)null);

                LockHeld = true;
                OnLockAcquired();
                return new ValueTask<IDistributedSynchronizationHandle?>(lockHandle);
            });
            LockProvider.CreateLock($"workflow-instance:{Instance.Id}").Returns(instanceLock);

            _serviceProvider = new ServiceCollection()
                .AddOptions()
                .Configure<DistributedLockingOptions>(options => options.LockAcquisitionTimeout = lockTimeout ?? options.LockAcquisitionTimeout)
                .AddSingleton<IRouteMatcher, ExactRouteMatcher>()
                .AddSingleton<IRouteTable>(new ListRouteTable([new(BookmarkPath)]))
                .AddSingleton<IStimulusHasher, FixedStimulusHasher>()
                .AddSingleton(bookmarkStore)
                .AddSingleton(workflowInstanceStore)
                .AddSingleton(workflowDefinitionService)
                .AddSingleton(Substitute.For<IHttpEndpointAuthorizationHandler>())
                .AddSingleton(WorkflowRunner)
                .AddSingleton(LockProvider)
                .BuildServiceProvider();
        }

        public WorkflowInstance Instance { get; }
        public IWorkflowRunner WorkflowRunner { get; } = Substitute.For<IWorkflowRunner>();
        public IDistributedLockProvider LockProvider { get; } = Substitute.For<IDistributedLockProvider>();
        public bool LockHeld { get; private set; }

        /// <summary>
        /// Simulates another run holding the instance lock for longer than the caller waits.
        /// </summary>
        public bool LockUnavailable { get; set; }

        /// <summary>
        /// The time the middleware was willing to wait for the instance lock.
        /// </summary>
        public TimeSpan RequestedLockTimeout { get; private set; }

        /// <summary>
        /// Runs right after the instance lock is acquired, to simulate changes made by another request while this one waited.
        /// </summary>
        public Action OnLockAcquired { get; set; } = () => { };

        public async Task<HttpContext> InvokeAsync(HttpWorkflowsMiddleware middleware)
        {
            var httpContext = new DefaultHttpContext
            {
                RequestServices = _serviceProvider
            };
            httpContext.Request.Path = BookmarkPath;
            httpContext.Request.Method = HttpMethod.Get.Method;

            await middleware.InvokeAsync(
                httpContext,
                _serviceProvider,
                Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { BasePath = null }),
                new EmptyHttpWorkflowLookupService());

            return httpContext;
        }
    }

    private class EmptyHttpWorkflowLookupService : IHttpWorkflowLookupService
    {
        public Task<HttpWorkflowLookupResult?> FindWorkflowAsync(string bookmarkHash, CancellationToken cancellationToken = default) => Task.FromResult<HttpWorkflowLookupResult?>(null);
    }

    private sealed class RecordingHttpWorkflowLookupService : IHttpWorkflowLookupService
    {
        public int Calls { get; private set; }

        public Task<HttpWorkflowLookupResult?> FindWorkflowAsync(string bookmarkHash, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<HttpWorkflowLookupResult?>(null);
        }
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
