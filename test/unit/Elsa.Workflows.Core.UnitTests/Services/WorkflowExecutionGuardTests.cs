using Elsa.Common;
using Elsa.Mediator.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Pipelines.WorkflowExecution;
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
    private int _middlewareCalls;

    public async Task InitializeAsync() => _context = (await new ActivityTestFixture(new WriteLine("guard"))
        .ConfigureServices(services => services.AddSingleton<IWorkflowExecutionGuard>(_guard)).BuildAsync()).WorkflowExecutionContext;
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

    private void Configure(IWorkflowExecutionPipelineBuilder builder) => builder.Use(Count);
    private WorkflowMiddlewareDelegate Count(WorkflowMiddlewareDelegate next) => async context =>
    {
        _middlewareCalls++;
        await next(context);
    };

    private WorkflowRunner CreateRunner(IWorkflowExecutionPipeline pipeline, INotificationSender notifications,
        ILoggerStateGenerator<WorkflowExecutionContext> loggerState) => new(_context.ServiceProvider, pipeline,
        Substitute.For<IWorkflowStateExtractor>(), Substitute.For<IWorkflowBuilderFactory>(), Substitute.For<IWorkflowGraphBuilder>(),
        Substitute.For<IIdentityGenerator>(), notifications, loggerState, Substitute.For<ICommitStateHandler>(), NullLogger<WorkflowRunner>.Instance);

    private sealed class TestGuard : IWorkflowExecutionGuard
    {
        public bool Owned { get; set; }
        public bool AuthorizeRunner { get; set; }
        public Ticket Ticket { get; } = new();
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
        public ValueTask RevalidateAsync(CancellationToken cancellationToken = default)
        {
            Revalidations++;
            if (Invalid)
            {
                throw new InvalidOperationException("Changed prepared invocation");
            }
            return ValueTask.CompletedTask;
        }
    }
}
