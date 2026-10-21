using Elsa.Common;
using Elsa.Mediator.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Compensation;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Models;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.Services;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Workflows.Core.UnitTests.Services;

public class WorkflowExecutionGuardTests : IAsyncLifetime
{
    private readonly TestGuard _guard = new();
    private WorkflowExecutionContext _context = null!;
    private ActivityExecutionContext _activityContext = null!;
    private int _middlewareCalls;

    public async Task InitializeAsync()
    {
        _activityContext = await new ActivityTestFixture(new WriteLine("guard"))
            .ConfigureServices(services => services.AddSingleton<IWorkflowExecutionGuard>(_guard)).BuildAsync();
        _context = _activityContext.WorkflowExecutionContext;
    }
    public async Task DisposeAsync() => await ((IAsyncDisposable)_context.ServiceProvider).DisposeAsync();

    [Theory]
    [InlineData("execute")]
    [InlineData("pipeline")]
    [InlineData("setup")]
    [InlineData("build")]
    [InlineData("cached-build")]
    [InlineData("reset")]
    [InlineData("insert")]
    [InlineData("replace")]
    public async Task PublicCompositionsGuardAtInvocationIncludingCachedAndChangedBuilders(string entryPoint)
    {
        var pipeline = new WorkflowExecutionPipeline(_context.ServiceProvider, Configure);
        var builder = new WorkflowExecutionPipelineBuilder(_context.ServiceProvider);
        Configure(builder);
        var cached = builder.Build();
        var invoke = entryPoint switch
        {
            "execute" => new Func<Task>(() => pipeline.ExecuteAsync(_context)),
            "pipeline" => () => pipeline.Pipeline(_context).AsTask(),
            "setup" => () => pipeline.Setup(Configure)(_context).AsTask(),
            "build" => () => builder.Build()(_context).AsTask(),
            "cached-build" => () => cached(_context).AsTask(),
            "reset" => () => builder.Reset().Use(Count).Build()(_context).AsTask(),
            "insert" => () => builder.Insert(0, Count).Build()(_context).AsTask(),
            "replace" => () => builder.Replace(0, Count).Build()(_context).AsTask(),
            _ => throw new ArgumentOutOfRangeException(nameof(entryPoint))
        };
        _guard.Owned = true;
        await Assert.ThrowsAsync<InvalidOperationException>(invoke);
        Assert.Equal(0, _middlewareCalls);
        _guard.Owned = false;
        await invoke();
        Assert.True(_middlewareCalls > 0);
    }

    [Fact]
    public async Task ConstructionProviderGuardCannotBeRemovedByBuilderProviderMutationOrForeignContext()
    {
        var foreign = (await new ActivityTestFixture(new WriteLine("foreign")).BuildAsync()).WorkflowExecutionContext;
        await using var foreignServices = (IAsyncDisposable)foreign.ServiceProvider;
        foreign.Id = _context.Id;
        var builder = new WorkflowExecutionPipelineBuilder(_context.ServiceProvider) { ServiceProvider = foreign.ServiceProvider };
        Configure(builder);
        _guard.Owned = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => builder.Build()(foreign).AsTask());
        Assert.Equal(0, _middlewareCalls);
    }

    [Fact]
    public async Task RunnerDeniesBeforeLoggerNotificationsAndMiddleware()
    {
        var notifications = Substitute.For<INotificationSender>();
        var loggerState = Substitute.For<ILoggerStateGenerator<WorkflowExecutionContext>>();
        var pipeline = new WorkflowExecutionPipeline(_context.ServiceProvider, Configure);
        var runner = CreateRunner(pipeline, notifications, loggerState);
        _guard.Owned = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(_context));
        Assert.Equal(0, _middlewareCalls);
        await notifications.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
        loggerState.DidNotReceiveWithAnyArgs().GenerateLoggerState(default!);
    }

    [Fact]
    public async Task RunnerRevalidatesAfterExecutingNotificationBeforeAnyMiddleware()
    {
        var notifications = Substitute.For<INotificationSender>();
        notifications.SendAsync(Arg.Any<INotification>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            if (call.Arg<INotification>() is WorkflowExecuting)
            {
                _guard.Ticket.Invalid = true;
            }
            return Task.CompletedTask;
        });
        _guard.AuthorizeRunner = true;
        var runner = CreateRunner(new WorkflowExecutionPipeline(_context.ServiceProvider, Configure), notifications,
            new WorkflowLoggerStateGenerator());
        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(_context));
        Assert.Equal(0, _middlewareCalls);
        Assert.Equal(1, _guard.Ticket.Revalidations);
        await notifications.Received(1).SendAsync(Arg.Is<INotification>(x => x is WorkflowExecuting), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunnerRejectsCompositionReplacementFromExecutingNotification()
    {
        var pipeline = new WorkflowExecutionPipeline(_context.ServiceProvider, Configure);
        var notifications = Substitute.For<INotificationSender>();
        notifications.SendAsync(Arg.Any<INotification>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            if (call.Arg<INotification>() is WorkflowExecuting)
            {
                pipeline.Setup(builder => builder.Use(Count));
            }
            return Task.CompletedTask;
        });
        _guard.AuthorizeRunner = true;
        var runner = CreateRunner(pipeline, notifications, new WorkflowLoggerStateGenerator());
        await Assert.ThrowsAnyAsync<InvalidOperationException>(() => runner.RunAsync(_context));
        Assert.Equal(0, _middlewareCalls);
    }

    [Fact]
    public void FrozenWorkflowAndActivityCompositionsRejectBeforeSetupCallbacks()
    {
        var callbacks = 0;
        var workflow = new WorkflowExecutionPipeline(_context.ServiceProvider, Configure);
        var activity = new ActivityExecutionPipeline(_context.ServiceProvider, builder => builder.Use(next => next));
        workflow.Freeze();
        activity.Freeze();
        Assert.Throws<InvalidOperationException>(() => workflow.Setup(builder =>
        {
            callbacks++;
            builder.Reset();
        }));
        Assert.Throws<InvalidOperationException>(() => activity.Setup(builder =>
        {
            callbacks++;
            builder.Reset();
        }));
        Assert.Equal(0, callbacks);
        // Mutability remains opt-in: ordinary hosts still execute their setup callbacks.
        new WorkflowExecutionPipeline(_context.ServiceProvider, Configure).Setup(_ => callbacks++);
        new ActivityExecutionPipeline(_context.ServiceProvider, _ => { }).Setup(_ => callbacks++);
        Assert.Equal(2, callbacks);
    }

    [Theory]
    [InlineData("execute")]
    [InlineData("pipeline")]
    [InlineData("setup")]
    [InlineData("build")]
    [InlineData("cached-build")]
    [InlineData("reset")]
    [InlineData("insert")]
    [InlineData("invoker-workflow")]
    [InlineData("invoker-context")]
    public async Task PublicActivityEntriesDenyOwnedBeforeMiddlewareAndRemainCompatibleWhenUnowned(string entryPoint)
    {
        // Reuse the fixture-owned graph member; a new activity has no ActivityNode.
        var activityContext = _activityContext;
        Assert.Same(activityContext.Activity, activityContext.ActivityNode.Activity);
        var calls = 0;
        ActivityMiddlewareDelegate CountActivity(ActivityMiddlewareDelegate next) => async context =>
        {
            calls++;
            await next(context);
        };
        var pipeline = new ActivityExecutionPipeline(_context.ServiceProvider, builder => builder.Use(CountActivity));
        var builder = new ActivityExecutionPipelinePipelineBuilder(_context.ServiceProvider);
        builder.Use(CountActivity);
        var cached = builder.Build();
        var invoker = new ActivityInvoker(pipeline, new ActivityLoggerStateGenerator(), NullLogger<ActivityInvoker>.Instance);
        var invoke = entryPoint switch
        {
            "execute" => new Func<Task>(() => pipeline.ExecuteAsync(activityContext)),
            "pipeline" => () => pipeline.Pipeline(activityContext).AsTask(),
            "setup" => () => pipeline.Setup(value => value.Use(CountActivity))(activityContext).AsTask(),
            "build" => () => builder.Build()(activityContext).AsTask(),
            "cached-build" => () => cached(activityContext).AsTask(),
            "reset" => () => builder.Reset().Use(CountActivity).Build()(activityContext).AsTask(),
            "insert" => () => builder.Insert(0, CountActivity).Build()(activityContext).AsTask(),
            "invoker-workflow" => async () => { await invoker.InvokeAsync(_context, activityContext.Activity); },
            "invoker-context" => () => invoker.InvokeAsync(activityContext),
            _ => throw new ArgumentOutOfRangeException(nameof(entryPoint))
        };
        _guard.Owned = true;
        await Assert.ThrowsAsync<InvalidOperationException>(invoke);
        Assert.Equal(0, calls);
        _guard.Owned = false;
        await invoke();
        Assert.True(calls > 0);
    }

    [Fact]
    public async Task RunnerRejectsTypedOpaqueAndIdentityOnlyScopesBeforeContextCreation()
    {
        foreach (var scope in new IActivity[]
                 {
                     new CompensationScope(),
                     new CompensationScope { Type = "Elsa.OtherActivity" },
                     new NotFoundActivity(missingTypeName: CompensationScope.ActivityTypeName) { OriginalActivityJson = "not-json" },
                     new NotFoundActivity(missingTypeName: CompensationScope.ActivityTypeName)
                 })
        {
            var scopeNode = AddGraphActivity(scope);
            var identityGenerator = Substitute.For<IIdentityGenerator>();
            var notifications = Substitute.For<INotificationSender>();
            var pipeline = new WorkflowExecutionPipeline(_context.ServiceProvider, Configure);
            var runner = CreateRunner(pipeline, notifications, new WorkflowLoggerStateGenerator(), identityGenerator);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(_context.WorkflowGraph));

            Assert.Equal(CompensationScope.DraftOnlyRefusalMessage, exception.Message);
            identityGenerator.DidNotReceiveWithAnyArgs().GenerateId();
            await notifications.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
            _context.WorkflowGraph.Nodes.Remove(scopeNode);
        }
    }

    [Fact]
    public async Task RunnerRejectsScopeAlreadyInExecutionContextBeforeNotifications()
    {
        var scopeNode = AddGraphActivity(new CompensationScope());
        var notifications = Substitute.For<INotificationSender>();
        var loggerState = Substitute.For<ILoggerStateGenerator<WorkflowExecutionContext>>();
        var runner = CreateRunner(new WorkflowExecutionPipeline(_context.ServiceProvider, Configure), notifications, loggerState);

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(_context));

        await notifications.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
        loggerState.DidNotReceiveWithAnyArgs().GenerateLoggerState(default!);
        _context.WorkflowGraph.Nodes.Remove(scopeNode);
    }

    [Fact]
    public async Task RunnerRechecksWholeGraphAfterNotificationsBeforeMiddleware()
    {
        var notifications = Substitute.For<INotificationSender>();
        notifications.SendAsync(Arg.Any<INotification>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            if (call.Arg<INotification>() is WorkflowExecuting)
                AddGraphActivity(new NotFoundActivity(missingTypeName: CompensationScope.ActivityTypeName) { OriginalActivityJson = "not-json" });

            return Task.CompletedTask;
        });
        var middlewareCalls = 0;
        WorkflowMiddlewareDelegate Count(WorkflowMiddlewareDelegate next) => async context =>
        {
            middlewareCalls++;
            await next(context);
        };
        var pipeline = new WorkflowExecutionPipeline(_context.ServiceProvider, builder => builder.Use(Count));
        var runner = CreateRunner(pipeline, notifications, new WorkflowLoggerStateGenerator());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(_context));

        Assert.Equal(CompensationScope.DraftOnlyRefusalMessage, exception.Message);
        Assert.Equal(0, middlewareCalls);
        await notifications.Received(1).SendAsync(Arg.Is<INotification>(x => x is WorkflowExecuting), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunnerRechecksAfterAuthorizationRevalidationBeforeMiddleware()
    {
        var middlewareCalls = 0;
        WorkflowMiddlewareDelegate Count(WorkflowMiddlewareDelegate next) => async context =>
        {
            middlewareCalls++;
            await next(context);
        };
        _guard.AuthorizeRunner = true;
        _guard.Ticket.OnRevalidate = () => AddGraphActivity(new CompensationScope());
        var pipeline = new WorkflowExecutionPipeline(_context.ServiceProvider, builder => builder.Use(Count));
        var runner = CreateRunner(pipeline, Substitute.For<INotificationSender>(), new WorkflowLoggerStateGenerator());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => runner.RunAsync(_context));

        Assert.Equal(CompensationScope.DraftOnlyRefusalMessage, exception.Message);
        Assert.Equal(1, _guard.Ticket.Revalidations);
        Assert.Equal(0, middlewareCalls);
    }

    [Fact]
    public async Task WorkflowPublicDelegatesCheckBeforeHostAuthorizationAndUseCurrentGraphAtInvocation()
    {
        var calls = 0;
        WorkflowMiddlewareDelegate Count(WorkflowMiddlewareDelegate next) => context =>
        {
            calls++;
            return next(context);
        };
        var pipeline = new WorkflowExecutionPipeline(_context.ServiceProvider, builder => builder.Use(Count));
        var configured = pipeline.Setup(builder => builder.Use(Count));
        var builder = new WorkflowExecutionPipelineBuilder(_context.ServiceProvider);
        builder.Use(Count);
        var built = builder.Build();
        var cachedBuilder = new WorkflowExecutionPipelineBuilder(_context.ServiceProvider);
        cachedBuilder.Use(Count);
        var cached = cachedBuilder.Build();
        WorkflowMiddlewareDelegate[] delegates = [pipeline.Pipeline, configured, built, cached];

        foreach (var activity in new IActivity[]
                 {
                     new CompensationScope(),
                     new NotFoundActivity(missingTypeName: CompensationScope.ActivityTypeName) { OriginalActivityJson = "not-json" }
                 })
        {
            var node = AddGraphActivity(activity);
            var authorizationCalls = _guard.AuthorizationCalls;
            foreach (var invoke in delegates)
            {
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => invoke(_context).AsTask());
                Assert.Equal(CompensationScope.DraftOnlyRefusalMessage, exception.Message);
            }

            Assert.Equal(authorizationCalls, _guard.AuthorizationCalls);
            Assert.Equal(0, calls);
            _context.WorkflowGraph.Nodes.Remove(node);
        }

        foreach (var invoke in delegates)
            await invoke(_context);
        Assert.True(calls > 0);
        var unrelatedUnknown = AddGraphActivity(new NotFoundActivity(missingTypeName: "Elsa.UnknownActivity") { OriginalActivityJson = "not-json" });
        foreach (var invoke in delegates)
            await invoke(_context);
        Assert.True(calls > delegates.Length);
        _context.WorkflowGraph.Nodes.Remove(unrelatedUnknown);
    }

    [Fact]
    public async Task WorkflowDelegateRechecksAfterHostAuthorizationMutatesGraph()
    {
        var calls = 0;
        WorkflowMiddlewareDelegate Count(WorkflowMiddlewareDelegate next) => context =>
        {
            calls++;
            return next(context);
        };
        var pipeline = new WorkflowExecutionPipeline(_context.ServiceProvider, builder => builder.Use(Count));
        _guard.OnAuthorize = () => AddGraphActivity(new NotFoundActivity(missingTypeName: CompensationScope.ActivityTypeName) { OriginalActivityJson = "not-json" });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.Pipeline(_context).AsTask());

        Assert.Equal(CompensationScope.DraftOnlyRefusalMessage, exception.Message);
        Assert.Equal(1, _guard.AuthorizationCalls);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task ActivityPublicDelegatesCheckBeforeHostAuthorizationAndUseCurrentGraphAtInvocation()
    {
        var calls = 0;
        ActivityMiddlewareDelegate Count(ActivityMiddlewareDelegate next) => context =>
        {
            calls++;
            return next(context);
        };
        var pipeline = new ActivityExecutionPipeline(_context.ServiceProvider, builder => builder.Use(Count));
        var configured = pipeline.Setup(builder => builder.Use(Count));
        var builder = new ActivityExecutionPipelinePipelineBuilder(_context.ServiceProvider);
        builder.Use(Count);
        var built = builder.Build();
        var cachedBuilder = new ActivityExecutionPipelinePipelineBuilder(_context.ServiceProvider);
        cachedBuilder.Use(Count);
        var cached = cachedBuilder.Build();
        ActivityMiddlewareDelegate[] delegates = [pipeline.Pipeline, configured, built, cached];

        foreach (var activity in new IActivity[]
                 {
                     new CompensationScope(),
                     new NotFoundActivity(missingTypeName: CompensationScope.ActivityTypeName) { OriginalActivityJson = "not-json" }
                 })
        {
            var node = AddGraphActivity(activity);
            var authorizationCalls = _guard.AuthorizationCalls;
            foreach (var invoke in delegates)
            {
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => invoke(_activityContext).AsTask());
                Assert.Equal(CompensationScope.DraftOnlyRefusalMessage, exception.Message);
            }

            Assert.Equal(authorizationCalls, _guard.AuthorizationCalls);
            Assert.Equal(0, calls);
            _context.WorkflowGraph.Nodes.Remove(node);
        }

        foreach (var invoke in delegates)
            await invoke(_activityContext);
        Assert.True(calls > 0);
    }

    [Fact]
    public async Task ActivityDelegateRechecksAfterHostAuthorizationMutatesGraph()
    {
        var calls = 0;
        ActivityMiddlewareDelegate Count(ActivityMiddlewareDelegate next) => context =>
        {
            calls++;
            return next(context);
        };
        var pipeline = new ActivityExecutionPipeline(_context.ServiceProvider, builder => builder.Use(Count));
        _guard.OnAuthorize = () => AddGraphActivity(new CompensationScope());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.Pipeline(_activityContext).AsTask());

        Assert.Equal(CompensationScope.DraftOnlyRefusalMessage, exception.Message);
        Assert.Equal(1, _guard.AuthorizationCalls);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task ActivityInvokerRejectsTypedAndOpaqueScopesBeforeCreatingChildExecutionContext()
    {
        var middlewareCalls = 0;
        ActivityMiddlewareDelegate Count(ActivityMiddlewareDelegate next) => context =>
        {
            middlewareCalls++;
            return next(context);
        };
        var invoker = new ActivityInvoker(new ActivityExecutionPipeline(_context.ServiceProvider, builder => builder.Use(Count)),
            new ActivityLoggerStateGenerator(), NullLogger<ActivityInvoker>.Instance);

        foreach (var activity in new IActivity[]
                 {
                     new CompensationScope(),
                     new NotFoundActivity(missingTypeName: CompensationScope.ActivityTypeName) { OriginalActivityJson = "not-json" }
                 })
        {
            var node = AddGraphActivity(activity);
            var contextCount = _context.ActivityExecutionContexts.Count;

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => invoker.InvokeAsync(_context, new WriteLine("target")));

            Assert.Equal(CompensationScope.DraftOnlyRefusalMessage, exception.Message);
            Assert.Equal(contextCount, _context.ActivityExecutionContexts.Count);
            await Assert.ThrowsAsync<InvalidOperationException>(() => invoker.InvokeAsync(_activityContext));
            Assert.Equal(0, middlewareCalls);
            _context.WorkflowGraph.Nodes.Remove(node);
        }

        await invoker.InvokeAsync(_activityContext);
        Assert.True(middlewareCalls > 0);
    }

    private ActivityNode AddGraphActivity(IActivity activity)
    {
        var node = new ActivityNode(activity, "test");
        _context.WorkflowGraph.Nodes.Add(node);
        return node;
    }

    private void Configure(IWorkflowExecutionPipelineBuilder builder) => builder.Use(Count);
    private WorkflowMiddlewareDelegate Count(WorkflowMiddlewareDelegate next) => async context =>
    {
        _middlewareCalls++;
        await next(context);
    };

    private WorkflowRunner CreateRunner(IWorkflowExecutionPipeline pipeline, INotificationSender notifications,
        ILoggerStateGenerator<WorkflowExecutionContext> loggerState, IIdentityGenerator? identityGenerator = null) => new(_context.ServiceProvider, pipeline,
        Substitute.For<IWorkflowStateExtractor>(), Substitute.For<IWorkflowBuilderFactory>(), Substitute.For<IWorkflowGraphBuilder>(),
        identityGenerator ?? Substitute.For<IIdentityGenerator>(), notifications, loggerState, Substitute.For<ICommitStateHandler>(), NullLogger<WorkflowRunner>.Instance);

    private sealed class TestGuard : IWorkflowExecutionGuard
    {
        public bool Owned { get; set; }
        public bool AuthorizeRunner { get; set; }
        public Ticket Ticket { get; } = new();
        public int AuthorizationCalls { get; private set; }
        public Action? OnAuthorize { get; set; }
        public ValueTask DemandUnownedAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            if (Owned)
            {
                throw new InvalidOperationException("Owned");
            }
            return ValueTask.CompletedTask;
        }
        public async ValueTask<IWorkflowExecutionAuthorization?> AuthorizeAsync(WorkflowExecutionContext context, WorkflowExecutionEntryPoint entryPoint)
        {
            AuthorizationCalls++;
            OnAuthorize?.Invoke();
            if (AuthorizeRunner && entryPoint == WorkflowExecutionEntryPoint.Runner)
            {
                return Ticket;
            }
            await DemandUnownedAsync(context.Id, context.CancellationToken);
            return null;
        }
    }

    private sealed class Ticket : IWorkflowExecutionAuthorization
    {
        public bool Invalid { get; set; }
        public int Revalidations { get; private set; }
        public Action? OnRevalidate { get; set; }
        public ValueTask RevalidateAsync(CancellationToken cancellationToken = default)
        {
            Revalidations++;
            OnRevalidate?.Invoke();
            if (Invalid)
            {
                throw new InvalidOperationException("Changed prepared invocation");
            }
            return ValueTask.CompletedTask;
        }
    }
}
