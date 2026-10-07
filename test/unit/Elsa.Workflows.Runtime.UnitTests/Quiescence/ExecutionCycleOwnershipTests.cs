using Elsa.Common;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Middleware.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime.Middleware.Workflows;
using Elsa.Workflows.Runtime.Services;
using Elsa.Workflows.Services;
using Elsa.Workflows.State;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Quiescence;

public class ExecutionCycleOwnershipTests : IAsyncLifetime
{
    private readonly ExecutionCycleRegistry _registry = new(Substitute.For<IIngressSourceRegistry>(), Substitute.For<ISystemClock>());
    private readonly WriteLine _activity = new("test");
    private WorkflowExecutionContext _context = null!;

    public async Task InitializeAsync() => _context = (await new ActivityTestFixture(_activity).ConfigureServices(services => services.AddSingleton<IExecutionCycleRegistry>(_registry)).BuildAsync()).WorkflowExecutionContext;
    public async Task DisposeAsync() => await ((IAsyncDisposable)_context.ServiceProvider).DisposeAsync();

    [Theory]
    [InlineData(WorkflowSubStatus.Finished)]
    [InlineData(WorkflowSubStatus.Suspended)]
    [InlineData(WorkflowSubStatus.Faulted)]
    public async Task RunnerTracksCustomFinalCommitForExtractedStatuses(WorkflowSubStatus subStatus)
    {
        var finalWrite = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ExecutionCycleHandle? handle = null;
        // This is ownership coverage for the state passed to a custom final commit. The extractor supplies
        // the statuses; the test does not simulate real suspension or handled-fault activity execution.
        var extractedState = State();
        extractedState.SubStatus = subStatus;
        extractedState.Status = subStatus == WorkflowSubStatus.Suspended ? WorkflowStatus.Running : WorkflowStatus.Finished;
        var extractor = Substitute.For<IWorkflowStateExtractor>();
        extractor.Extract(_context).Returns(extractedState);
        var commit = Substitute.For<ICommitStateHandler>();
        commit.CommitAsync(_context, Arg.Any<WorkflowState>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            Assert.Same(extractedState, call.Arg<WorkflowState>());
            Assert.Same(handle, Assert.Single(_registry.ListActiveCycles()));
            writing.SetResult();
            return finalWrite.Task;
        });
        var runner = CreateRunner(new TrackingPipeline(_registry, context =>
        {
            handle = Assert.Single(_registry.ListActiveCycles());
            return ValueTask.CompletedTask;
        }), commit, extractor);

        var run = runner.RunAsync(_context);
        try
        {
            await writing.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(1, _registry.ActiveCount);
            Assert.False(handle!.Disposed.IsCompleted);
            handle.Cancel();
            Assert.Equal(1, _registry.ActiveCount);
            Assert.False(handle.Disposed.IsCompleted);
        }
        finally
        {
            finalWrite.TrySetResult();
            await run.WaitAsync(TimeSpan.FromSeconds(5));
        }
        Assert.Equal(0, _registry.ActiveCount);
        Assert.True(handle!.Disposed.IsCompletedSuccessfully);
        Assert.False(_context.TransientProperties.ContainsKey(ExecutionCycleTrackingMiddleware.ExecutionCycleHandleKey));
    }

    [Theory]
    [InlineData("pipeline")]
    [InlineData("cancellation")]
    [InlineData("checkpoint")]
    [InlineData("extraction")]
    [InlineData("notification")]
    [InlineData("final-write")]
    public async Task RunnerFailureUnwindsOwnershipAndPreservesOriginalException(string failure)
    {
        Exception original = failure == "cancellation" ? new OperationCanceledException("cancelled") : new InvalidOperationException(failure);
        var commit = Substitute.For<ICommitStateHandler>();
        var extractor = Substitute.For<IWorkflowStateExtractor>();
        extractor.Extract(_context).Returns(_ => failure == "extraction" ? throw original : State());
        var notifications = Substitute.For<INotificationSender>();
        notifications.SendAsync(Arg.Any<INotification>(), Arg.Any<CancellationToken>()).Returns(call =>
            failure == "notification" && call.Arg<INotification>() is WorkflowExecuted ? Task.FromException(original) : Task.CompletedTask);
        commit.CommitAsync(_context, Arg.Any<WorkflowState>(), Arg.Any<CancellationToken>()).Returns(_ =>
            failure == "final-write" ? Task.FromException(original) : Task.CompletedTask);
        if (failure == "checkpoint")
        {
            var contextCommit = _context.ServiceProvider.GetService(typeof(ICommitStateHandler)) as ICommitStateHandler;
            contextCommit!.CommitAsync(_context, Arg.Any<CancellationToken>()).Returns(Task.FromException(original));
        }
        ExecutionCycleHandle? handle = null;
        var pipeline = new TrackingPipeline(_registry, async context =>
        {
            handle = Assert.Single(_registry.ListActiveCycles());
            if (failure is "pipeline" or "cancellation")
            {
                throw original;
            }
            if (failure == "checkpoint")
            {
                await context.CommitAsync();
            }
        });
        var runner = CreateRunner(pipeline, commit, extractor, notifications);

        var actual = await Record.ExceptionAsync(() => runner.RunAsync(_context));

        Assert.Same(original, actual);
        Assert.Equal(0, _registry.ActiveCount);
        Assert.True(handle!.Disposed.IsCompletedSuccessfully);
        Assert.False(_context.TransientProperties.ContainsKey(ExecutionCycleTrackingMiddleware.ExecutionCycleHandleKey));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DirectPipelineBoundaryIsExtendedOnlyByExplicitOwner(bool explicitOwner)
    {
        ExecutionCycleHandle? handle = null;
        using var owner = explicitOwner ? WorkflowExecutionScope.Begin(_context) : null;
        var pipeline = new TrackingPipeline(_registry, _ =>
        {
            handle = Assert.Single(_registry.ListActiveCycles());
            return ValueTask.CompletedTask;
        });
        await pipeline.ExecuteAsync(_context);

        Assert.Equal(explicitOwner ? 1 : 0, _registry.ActiveCount);
        Assert.Equal(!explicitOwner, handle!.Disposed.IsCompleted);
        if (explicitOwner)
        {
            // An arbitrary caller write belongs inside this scope, without invoking any commit handler.
            var written = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var trailingWrite = Task.Run(() =>
            {
                Assert.Equal(1, _registry.ActiveCount);
                written.SetResult();
            });
            await written.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await trailingWrite;
            owner!.Dispose();
        }
        Assert.Equal(0, _registry.ActiveCount);
    }

    [Fact]
    public async Task NestedPipelineReusesHandleAndChildPipelineHasSeparateHandle()
    {
        using var owner = WorkflowExecutionScope.Begin(_context);
        ExecutionCycleHandle? outerHandle = null;
        var nested = new TrackingPipeline(_registry, context =>
        {
            Assert.Same(outerHandle, Assert.Single(_registry.ListActiveCycles()));
            return ValueTask.CompletedTask;
        });
        var pipeline = new TrackingPipeline(_registry, async context =>
        {
            outerHandle = Assert.Single(_registry.ListActiveCycles());
            await nested.ExecuteAsync(context);
            var child = await WorkflowExecutionContext.CreateAsync(context.ServiceProvider, context.WorkflowGraph, context.Id);
            await new TrackingPipeline(_registry, _ =>
            {
                Assert.Equal(2, _registry.ActiveCount);
                Assert.Contains(outerHandle, _registry.ListActiveCycles());
                return ValueTask.CompletedTask;
            }).ExecuteAsync(child);
            Assert.Same(outerHandle, Assert.Single(_registry.ListActiveCycles()));
        });
        await pipeline.ExecuteAsync(_context);
        Assert.Same(outerHandle, Assert.Single(_registry.ListActiveCycles()));
        owner.Dispose();
        Assert.Equal(0, _registry.ActiveCount);
    }

    [Fact]
    public async Task ActivityTestRunnerReleasesCycleWithoutFinalCommit()
    {
        var activity = _activity;
        var pipeline = new TrackingPipeline(_registry, async context =>
        {
            Assert.Equal(1, _registry.ActiveCount);
            var activityContext = await context.CreateActivityExecutionContextAsync(activity);
            context.AddActivityExecutionContext(activityContext);
        });
        var identity = Substitute.For<IIdentityGenerator>();
        identity.GenerateId().Returns("activity-test");
        var runner = new ActivityTestRunner(_context.ServiceProvider, pipeline, identity);

        await runner.RunAsync(_context.WorkflowGraph, activity);

        Assert.Equal(0, _registry.ActiveCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LocalCancellationTracksThroughCallerSave(bool failSave)
    {
        var manager = Substitute.For<IWorkflowInstanceManager>();
        var definitions = Substitute.For<IWorkflowDefinitionService>();
        var state = PersistableState();
        var instance = new WorkflowInstance { Id = state.Id, Status = WorkflowStatus.Running, DefinitionVersionId = state.DefinitionVersionId, WorkflowState = state };
        manager.FindByIdAsync(state.Id, Arg.Any<CancellationToken>()).Returns(instance);
        definitions.TryFindWorkflowGraphAsync(Arg.Any<WorkflowDefinitionHandle>(), Arg.Any<CancellationToken>())
            .Returns(new WorkflowGraphFindResult(new WorkflowDefinition { Id = state.DefinitionVersionId }, _context.WorkflowGraph));
        var canceler = CreateCanceler();
        var save = new SaveGate(_registry, instance, failSave);
        manager.SaveAsync(Arg.Any<WorkflowState>(), Arg.Any<CancellationToken>()).Returns(_ => save.SaveAsync());
        var client = new LocalWorkflowClient(state.Id, manager, definitions, Substitute.For<IWorkflowRunner>(), canceler,
            Substitute.For<IWorkflowActivationGate>(), new WorkflowStateMapper(), NullLogger<LocalWorkflowClient>.Instance);

        await AssertTrailingWriteAsync(() => client.CancelAsync(), save);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task LegacyHostTracksThroughTrailingPersistence(bool cancel, bool failSave)
    {
        var state = PersistableState();
        var instance = new WorkflowInstance { Id = state.Id, WorkflowState = state };
        var manager = Substitute.For<IWorkflowInstanceManager>();
        var save = new SaveGate(_registry, instance, failSave);
        manager.SaveAsync(Arg.Any<WorkflowState>(), Arg.Any<CancellationToken>()).Returns(_ => save.SaveAsync());
        var runner = Substitute.For<IWorkflowRunner>();
        runner.RunAsync(Arg.Any<WorkflowGraph>(), Arg.Any<WorkflowState>(), Arg.Any<RunWorkflowOptions>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var context = await WorkflowExecutionContext.CreateAsync(_context.ServiceProvider, _context.WorkflowGraph, state.Id);
                using var owner = WorkflowExecutionScope.Begin(context);
                await new TrackingPipeline(_registry, _ => ValueTask.CompletedTask).ExecuteAsync(context);
                return new RunWorkflowResult(context, state, context.Workflow, null, Journal.Empty);
            });
        await using var hostServices = new ServiceCollection()
            .AddScoped(_ => runner)
            .AddScoped(_ => CreateCanceler())
            .AddScoped(_ => manager)
            .BuildServiceProvider();
        // Exercise the retained obsolete entrypoints because their additional persistence still belongs to this attempt.
#pragma warning disable CS0618
        var host = new WorkflowHost(hostServices.GetRequiredService<IServiceScopeFactory>(), _context.WorkflowGraph, state, NullLogger<WorkflowHost>.Instance);
#pragma warning restore CS0618
        await AssertTrailingWriteAsync(async () =>
        {
            if (cancel)
            {
                await host.CancelWorkflowAsync();
            }
            else
            {
                await host.RunWorkflowAsync();
            }
        }, save);
    }

    private WorkflowState PersistableState()
    {
        var state = _context.ServiceProvider.GetRequiredService<IWorkflowStateExtractor>().Extract(_context);
        state.DefinitionId = "definition";
        state.DefinitionVersionId = "definition-v1";
        return state;
    }

    private IWorkflowCanceler CreateCanceler() => new WorkflowCanceler(new TrackingPipeline(_registry, _ => ValueTask.CompletedTask),
        _context.ServiceProvider.GetRequiredService<IWorkflowStateExtractor>(), Substitute.For<IMediator>(), _context.ServiceProvider);

    private async Task AssertTrailingWriteAsync(Func<Task> execute, SaveGate save)
    {
        var operation = execute();
        ExecutionCycleHandle? handle = null;
        try
        {
            handle = await save.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Same(handle, Assert.Single(_registry.ListActiveCycles()));
            Assert.False(handle.Disposed.IsCompleted);
            Assert.False(operation.IsCompleted);
        }
        finally
        {
            save.Continue.TrySetResult();
        }
        var error = await Record.ExceptionAsync(() => operation.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Same(save.Failure, error);
        Assert.Equal(0, _registry.ActiveCount);
        Assert.True(handle!.Disposed.IsCompletedSuccessfully);
    }

    private sealed class SaveGate(IExecutionCycleRegistry registry, WorkflowInstance instance, bool fail)
    {
        public TaskCompletionSource<ExecutionCycleHandle> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Exception? Failure { get; } = fail ? new InvalidOperationException("save failed") : null;

        public async Task<WorkflowInstance> SaveAsync()
        {
            Started.SetResult(Assert.Single(registry.ListActiveCycles()));
            await Continue.Task;
            if (Failure != null)
            {
                throw Failure;
            }
            return instance;
        }
    }

    private WorkflowState State() => new() { Id = _context.Id, Status = _context.Status, SubStatus = _context.SubStatus };

    private WorkflowRunner CreateRunner(IWorkflowExecutionPipeline pipeline, ICommitStateHandler commit, IWorkflowStateExtractor? extractor = null, INotificationSender? notifications = null)
    {
        if (extractor == null)
        {
            extractor = Substitute.For<IWorkflowStateExtractor>();
            extractor.Extract(_context).Returns(_ => State());
        }
        notifications ??= Substitute.For<INotificationSender>();
        return new(_context.ServiceProvider, pipeline, extractor, Substitute.For<IWorkflowBuilderFactory>(),
            Substitute.For<IWorkflowGraphBuilder>(), Substitute.For<IIdentityGenerator>(), notifications,
            new WorkflowLoggerStateGenerator(), commit, NullLogger<WorkflowRunner>.Instance);
    }

    private sealed class TrackingPipeline(IExecutionCycleRegistry registry, WorkflowMiddlewareDelegate next) : IWorkflowExecutionPipeline
    {
        public Action<IWorkflowExecutionPipelineBuilder> ConfigurePipelineBuilder => builder => builder.UseExecutionCycleTracking().UseDefaultActivityScheduler();
        public WorkflowMiddlewareDelegate Pipeline => context => new ExecutionCycleTrackingMiddleware(next, registry).InvokeAsync(context);
        public WorkflowMiddlewareDelegate Setup(Action<IWorkflowExecutionPipelineBuilder> setup) => Pipeline;
        public async Task ExecuteAsync(WorkflowExecutionContext context) => await Pipeline(context);
    }
}
