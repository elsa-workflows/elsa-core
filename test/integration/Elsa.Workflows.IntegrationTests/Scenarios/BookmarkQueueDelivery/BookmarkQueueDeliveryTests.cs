using Elsa.Common.DistributedHosting.DistributedLocks;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Distributed;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.State;
using Medallion.Threading;
using Medallion.Threading.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Workflows.IntegrationTests.Scenarios.BookmarkQueueDelivery;

public class BookmarkQueueDeliveryTests
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);

    /// <summary>
    /// An early stimulus remains queued until both its bookmark and workflow state are saved, then resumes the real workflow.
    /// </summary>
    [Fact]
    public async Task EnqueueBeforeBookmark_WakesAfterStateSave_AndDeliversWithoutFallback()
    {
        await using var app = await DeliveryApp.CreateAsync();
        await app.EnqueueAsync();
        await app.ConsumeSignalAsync();
        await app.Processor.ProcessAsync();
        Assert.Single(await app.QueueItemsAsync());
        app.Signaler.Count = 0;

        var starting = app.StartAsync();
        await app.Gate.Indexed.Task.WaitAsync(Deadline);
        try
        {
            Assert.Single(await app.Services.GetRequiredService<IBookmarkStore>().FindManyAsync(new BookmarkFilter { WorkflowInstanceId = "delivery-instance" }));
            Assert.Equal(0, app.Signaler.Count);
        }
        finally
        {
            app.Gate.Release.TrySetResult();
            await starting.WaitAsync(Deadline);
        }

        Assert.Equal(1, app.Signaler.Count);
        await app.ConsumeSignalAsync();
        await app.Processor.ProcessAsync();
        await app.AssertFinishedAsync();
        Assert.Equal(1, app.Signaler.Count);
    }

    /// <summary>
    /// Enqueueing after bookmark creation keeps the existing immediate delivery path and produces no self-wakeup on completion.
    /// </summary>
    [Fact]
    public async Task EnqueueAfterBookmark_DeliversWithoutFallback()
    {
        await using var app = await DeliveryApp.CreateAsync();
        app.Gate.Release.TrySetResult();
        await app.StartAsync();
        await app.ConsumeSignalAsync();
        app.Signaler.Count = 0;

        await app.EnqueueAsync();
        await app.ConsumeSignalAsync();
        await app.Processor.ProcessAsync();

        await app.AssertFinishedAsync();
        Assert.Equal(1, app.Signaler.Count);
    }

    /// <summary>
    /// Explicitly saving a previously missing bookmark wakes a queued stimulus and resumes its actual workflow.
    /// </summary>
    [Fact]
    public async Task BookmarkManagerSave_WakesWaitingStimulus()
    {
        await using var app = await DeliveryApp.CreateAsync();
        app.Gate.Release.TrySetResult();
        await app.StartAsync();
        await app.ConsumeSignalAsync();
        var store = app.Services.GetRequiredService<IBookmarkStore>();
        var bookmark = Assert.Single(await store.FindManyAsync(new BookmarkFilter { WorkflowInstanceId = "delivery-instance" }));
        await store.DeleteAsync(new BookmarkFilter { BookmarkId = bookmark.Id });
        await app.EnqueueAsync();
        await app.ConsumeSignalAsync();
        await app.Processor.ProcessAsync();
        Assert.Single(await app.QueueItemsAsync());
        app.Signaler.Count = 0;

        await app.Services.GetRequiredService<IBookmarkManager>().SaveAsync(bookmark);
        await app.ConsumeSignalAsync();
        await app.Processor.ProcessAsync();

        await app.AssertFinishedAsync();
        Assert.Equal(1, app.Signaler.Count);
    }

    /// <summary>
    /// A real bookmark-free workflow completes without creating queue work.
    /// </summary>
    [Fact]
    public async Task BookmarkFreeWorkflow_DoesNotWakeQueue()
    {
        await using var app = await DeliveryApp.CreateAsync();
        var result = await app.Services.GetRequiredService<IWorkflowRunner>().RunAsync(new Sequence());
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Equal(0, app.Signaler.Count);
    }

    /// <summary>
    /// The post-save wakeup is not delayed until unrelated deferred work or the final WorkflowStateCommitted event finishes.
    /// </summary>
    [Fact]
    public async Task NewBookmark_WakesBeforeDeferredTasksFinish()
    {
        await using var app = await DeliveryApp.CreateAsync();
        app.Gate.DeferWork = true;
        app.Gate.Release.TrySetResult();
        var starting = app.StartAsync();
        await app.Gate.DeferredEntered.Task.WaitAsync(Deadline);
        try
        {
            Assert.Equal(1, app.Signaler.Count);
            var client = await app.Services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync("delivery-instance");
            Assert.Single((await client.ExportStateAsync()).Bookmarks);
        }
        finally
        {
            app.Gate.DeferredRelease.TrySetResult();
            await starting.WaitAsync(Deadline);
        }
    }

    /// <summary>
    /// Failed or cancelled workflow-state persistence never announces newly indexed bookmarks as ready for delivery.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedStateSave_DoesNotWakeQueue(bool cancelled)
    {
        await using var app = await DeliveryApp.CreateAsync();
        app.Gate.Release.TrySetResult();
        var graph = await app.Services.GetRequiredService<IWorkflowGraphBuilder>().BuildAsync(new Workflow { Root = new Sequence() });
        var context = await WorkflowExecutionContext.CreateAsync(app.Services, graph, "failed-instance", [], [], DateTimeOffset.UtcNow);
        context.Bookmarks.Add(new Bookmark { Id = "new-bookmark" });
        var manager = Substitute.For<IWorkflowInstanceManager>();
        Exception failure = cancelled ? new OperationCanceledException() : new InvalidOperationException("Controlled state-save failure");
        manager.SaveAsync(Arg.Any<WorkflowState>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<WorkflowInstance>(failure));
        var handler = new DefaultCommitStateHandler(manager, app.Services.GetRequiredService<IBookmarksPersister>(),
            Substitute.For<IVariablePersistenceManager>(), app.Services.GetRequiredService<IMediator>(),
            Substitute.For<ILogRecordSink<ActivityExecutionRecord>>(), Substitute.For<ILogRecordSink<WorkflowExecutionLogRecord>>());

        var observed = await Record.ExceptionAsync(() => handler.CommitAsync(context, new WorkflowState { Id = "failed-instance" }));

        Assert.Same(failure, observed);
        Assert.Equal(0, app.Signaler.Count);
    }

    /// <summary>
    /// A second worker preserves its wakeup while the first finishes an earlier pass that could not yet match the stimulus.
    /// </summary>
    [Fact]
    public async Task CompetingWorker_DeliversAfterEarlierPassReleasesLock()
    {
        await using var app = await DeliveryApp.CreateAsync();
        using var locks = new WorkerLocks();
        var paused = new PausedProcessor(app.Processor);
        await using var firstServices = new ServiceCollection().AddSingleton<IBookmarkQueueProcessor>(paused).BuildServiceProvider();
        var first = new PassWorker(locks.Provider, new BookmarkQueueSignaler(), firstServices.GetRequiredService<IServiceScopeFactory>());
        var second = new PassWorker(locks.ObservedProvider, app.Signaler, app.Services.GetRequiredService<IServiceScopeFactory>());
        await app.EnqueueAsync();
        await app.ConsumeSignalAsync();
        var firstPass = first.RunOnceAsync();
        await paused.Processed.Task.WaitAsync(Deadline);
        try
        {
            Assert.Single(await app.QueueItemsAsync());
            app.Gate.Release.TrySetResult();
            await app.StartAsync();
            await app.ConsumeSignalAsync();
            var secondPass = second.RunOnceAsync();
            await locks.Attempted.Task.WaitAsync(Deadline);
            paused.Release.TrySetResult();
            await Task.WhenAll(firstPass, secondPass).WaitAsync(Deadline);
            await app.AssertFinishedAsync();
        }
        finally
        {
            paused.Release.TrySetResult();
            await firstPass.WaitAsync(Deadline);
        }
    }

    /// <summary>
    /// Cancellation interrupts a worker waiting behind another owner and never starts queue processing.
    /// </summary>
    [Fact]
    public async Task CompetingWorker_CancelsWhileWaitingForLock()
    {
        using var locks = new WorkerLocks();
        var processor = Substitute.For<IBookmarkQueueProcessor>();
        await using var services = new ServiceCollection().AddSingleton(processor).BuildServiceProvider();
        var worker = new PassWorker(locks.ObservedProvider, new BookmarkQueueSignaler(), services.GetRequiredService<IServiceScopeFactory>());
        await using var held = await locks.Provider.AcquireLockAsync(nameof(DistributedBookmarkQueueWorker));
        using var cancellation = new CancellationTokenSource();

        var waiting = worker.RunOnceAsync(cancellation.Token);
        await locks.Attempted.Task.WaitAsync(Deadline);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting.WaitAsync(Deadline));
        await processor.DidNotReceiveWithAnyArgs().ProcessAsync(default);
    }

    /// <summary>
    /// Queue delivery runs through the real mediator, stores, resumer and workflow runner; recurring jobs are never started.
    /// </summary>
    private sealed class DeliveryApp(ServiceProvider services, SignalCounter signaler, IndexGate gate) : IAsyncDisposable
    {
        public ServiceProvider Services { get; } = services;
        public SignalCounter Signaler { get; } = signaler;
        public IndexGate Gate { get; } = gate;
        public IBookmarkQueueProcessor Processor => Services.GetRequiredService<IBookmarkQueueProcessor>();

        /// <summary>
        /// Creates an isolated application and populates workflow definitions without starting background timers.
        /// </summary>
        public static async Task<DeliveryApp> CreateAsync()
        {
            var signaler = new SignalCounter();
            var gate = new IndexGate();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddElsa(elsa => elsa.UseWorkflowManagement().UseWorkflowRuntime(runtime =>
                runtime.DistributedLockProvider = _ => new NoopDistributedSynchronizationProvider()).AddWorkflow<DeliveryWorkflow>());
            services.AddSingleton<IBookmarkQueueSignaler>(signaler);
            services.AddSingleton(gate);
            services.AddNotificationHandler<PauseAfterIndexing>();
            var provider = services.BuildServiceProvider();
            await provider.GetRequiredService<IRegistriesPopulator>().PopulateAsync();
            return new(provider, signaler, gate);
        }

        public async Task StartAsync()
        {
            var client = await Services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync("delivery-instance");
            await client.CreateAndRunInstanceAsync(new() { WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId("queue-delivery") });
        }

        public Task EnqueueAsync() => Services.GetRequiredService<IBookmarkQueue>().EnqueueAsync(new() { WorkflowInstanceId = "delivery-instance" });
        public Task ConsumeSignalAsync() => Signaler.AwaitAsync(default).WaitAsync(Deadline);
        public Task<IEnumerable<BookmarkQueueItem>> QueueItemsAsync() => Services.GetRequiredService<IBookmarkQueueStore>().FindManyAsync(new BookmarkQueueFilter());

        /// <summary>
        /// Checks persisted workflow completion and deletion of the successfully delivered queued stimulus.
        /// </summary>
        public async Task AssertFinishedAsync()
        {
            var client = await Services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync("delivery-instance");
            Assert.Equal(WorkflowStatus.Finished, (await client.ExportStateAsync()).Status);
            Assert.Empty(await QueueItemsAsync());
        }

        public ValueTask DisposeAsync() => Services.DisposeAsync();
    }

    public sealed class DeliveryWorkflow : WorkflowBase
    {
        /// <inheritdoc />
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.WithDefinitionId("queue-delivery");
            builder.Root = new Event("deliver");
        }
    }

    private sealed class SignalCounter : IBookmarkQueueSignaler
    {
        private readonly BookmarkQueueSignaler _inner = new();
        private int _count;
        public int Count { get => Volatile.Read(ref _count); set => Volatile.Write(ref _count, value); }
        public Task AwaitAsync(CancellationToken cancellationToken) => _inner.AwaitAsync(cancellationToken);
        public Task TriggerAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _count);
            return _inner.TriggerAsync(cancellationToken);
        }
    }

    private sealed class IndexGate
    {
        public bool DeferWork { get; set; }
        public TaskCompletionSource Indexed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource DeferredEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource DeferredRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class PauseAfterIndexing(IndexGate gate) : INotificationHandler<WorkflowBookmarksIndexed>
    {
        /// <summary>
        /// Pauses after the bookmark write, before the owning workflow state save.
        /// </summary>
        public async Task HandleAsync(WorkflowBookmarksIndexed notification, CancellationToken cancellationToken)
        {
            if (notification.IndexedWorkflowBookmarks.AddedBookmarks.Count == 0) return;
            gate.Indexed.TrySetResult();
            await gate.Release.Task.WaitAsync(cancellationToken);
            if (gate.DeferWork)
                notification.IndexedWorkflowBookmarks.WorkflowExecutionContext.DeferTask(async () =>
                {
                    gate.DeferredEntered.TrySetResult();
                    await gate.DeferredRelease.Task.WaitAsync(cancellationToken);
                });
        }
    }

    private sealed class PausedProcessor(IBookmarkQueueProcessor inner) : IBookmarkQueueProcessor
    {
        public TaskCompletionSource Processed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task ProcessAsync(CancellationToken cancellationToken)
        {
            await inner.ProcessAsync(cancellationToken);
            Processed.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class PassWorker(IDistributedLockProvider locks, IBookmarkQueueSignaler signaler, IServiceScopeFactory scopes)
        : DistributedBookmarkQueueWorker(locks, signaler, scopes, NullLogger<DistributedBookmarkQueueWorker>.Instance)
    {
        public Task RunOnceAsync(CancellationToken cancellationToken = default) => base.ProcessAsync(cancellationToken);
    }

    private sealed class WorkerLocks : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("elsa-queue-delivery-");
        public IDistributedLockProvider Provider { get; }
        public IDistributedLockProvider ObservedProvider { get; }
        public TaskCompletionSource Attempted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public WorkerLocks()
        {
            Provider = new FileDistributedSynchronizationProvider(_directory);
            ObservedProvider = Substitute.For<IDistributedLockProvider>();
            ObservedProvider.CreateLock(Arg.Any<string>()).Returns(call =>
            {
                Attempted.TrySetResult();
                return Provider.CreateLock(call.Arg<string>());
            });
        }
        public void Dispose() => _directory.Delete(true);
    }
}
