using Elsa.Mediator.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.State;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class DefaultCommitStateHandlerTests
{
    [Test]
    public async Task CommitAsync_ExecutesPersistenceInsideCommitTransaction()
    {
        await using var fixture = await CommitTestFixture.CreateAsync();
        fixture.WorkflowInstanceManager.SaveAsync(fixture.WorkflowState, Arg.Any<CancellationToken>()).Returns(fixture.WorkflowInstance);
        var bookmarksPersistedInsideTransaction = false;
        var activityLogsPersistedInsideTransaction = false;
        var workflowLogsPersistedInsideTransaction = false;
        var variablesSavedInsideTransaction = false;
        var workflowSavedInsideTransaction = false;
        fixture.BookmarkPersister.When(x => x.PersistBookmarksAsync(Arg.Any<UpdateBookmarksRequest>())).Do(_ => bookmarksPersistedInsideTransaction = fixture.Transaction.IsExecuting);
        fixture.ActivityExecutionLogSink.When(x => x.PersistExecutionLogsAsync(fixture.WorkflowExecutionContext, Arg.Any<CancellationToken>())).Do(_ => activityLogsPersistedInsideTransaction = fixture.Transaction.IsExecuting);
        fixture.WorkflowExecutionLogSink.When(x => x.PersistExecutionLogsAsync(fixture.WorkflowExecutionContext, Arg.Any<CancellationToken>())).Do(_ => workflowLogsPersistedInsideTransaction = fixture.Transaction.IsExecuting);
        fixture.VariablePersistenceManager.When(x => x.SaveVariablesAsync(fixture.WorkflowExecutionContext)).Do(_ => variablesSavedInsideTransaction = fixture.Transaction.IsExecuting);
        fixture.WorkflowInstanceManager.When(x => x.SaveAsync(fixture.WorkflowState, Arg.Any<CancellationToken>())).Do(_ => workflowSavedInsideTransaction = fixture.Transaction.IsExecuting);

        await fixture.Handler.CommitAsync(fixture.WorkflowExecutionContext, fixture.WorkflowState);

        await Assert.That(bookmarksPersistedInsideTransaction).IsTrue();
        await Assert.That(activityLogsPersistedInsideTransaction).IsTrue();
        await Assert.That(workflowLogsPersistedInsideTransaction).IsTrue();
        await Assert.That(variablesSavedInsideTransaction).IsTrue();
        await Assert.That(workflowSavedInsideTransaction).IsTrue();
        await Assert.That(fixture.Transaction.Completed).IsTrue();
        await Assert.That(fixture.ActivityExecutionContext.IsDirty).IsFalse();
        await fixture.NotificationSender.Received(1).SendAsync(
            Arg.Is<WorkflowStateCommitted>(x => x.WorkflowInstance == fixture.WorkflowInstance && x.WorkflowState == fixture.WorkflowState),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CommitAsync_WhenPersistenceFails_DoesNotClearExecutionLogOrPublishCommittedNotification()
    {
        await using var fixture = await CommitTestFixture.CreateAsync();
        fixture.WorkflowExecutionContext.AddExecutionLogEntry("Started");
        fixture.WorkflowInstanceManager.SaveAsync(fixture.WorkflowState, Arg.Any<CancellationToken>()).Returns<Task<WorkflowInstance>>(_ => throw new InvalidOperationException("state save failed"));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => fixture.Handler.CommitAsync(fixture.WorkflowExecutionContext, fixture.WorkflowState));

        await Assert.That(fixture.Transaction.Executed).IsTrue();
        await Assert.That(fixture.Transaction.Completed).IsFalse();
        await Assert.That(fixture.ActivityExecutionContext.IsDirty).IsTrue();
        await Assert.That(fixture.WorkflowExecutionContext.ExecutionLog).IsNotEmpty();
        await fixture.NotificationSender.DidNotReceive().SendAsync(Arg.Any<WorkflowStateCommitted>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CommitAsync_WhenBufferedNotificationFlushFails_PublishesCommittedNotification()
    {
        await using var fixture = await CommitTestFixture.CreateAsync();
        var bufferedNotificationSender = new WorkflowCommitNotificationSender(fixture.Mediator, fixture.NotificationBuffer);
        var bufferedNotification = new TestNotification();
        fixture.WorkflowInstanceManager.SaveAsync(fixture.WorkflowState, Arg.Any<CancellationToken>()).Returns(fixture.WorkflowInstance);
        fixture.ActivityExecutionLogSink
            .PersistExecutionLogsAsync(fixture.WorkflowExecutionContext, Arg.Any<CancellationToken>())
            .Returns(_ => bufferedNotificationSender.SendAsync(bufferedNotification));
        fixture.Mediator
            .SendAsync(bufferedNotification, Arg.Any<IEventPublishingStrategy?>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("Buffered handler failed"));

        await Assert.ThrowsExactlyAsync<AggregateException>(() => fixture.Handler.CommitAsync(fixture.WorkflowExecutionContext, fixture.WorkflowState));

        await fixture.NotificationSender.Received(1).SendAsync(
            Arg.Is<WorkflowStateCommitted>(x => x.WorkflowInstance == fixture.WorkflowInstance && x.WorkflowState == fixture.WorkflowState),
            Arg.Any<CancellationToken>());
    }

    private class CommitTestFixture : IAsyncDisposable
    {
        private CommitTestFixture(ActivityExecutionContext activityExecutionContext)
        {
            ActivityExecutionContext = activityExecutionContext;
            ActivityExecutionContext.Taint();
            WorkflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;
            WorkflowExecutionContext.AddActivityExecutionContext(activityExecutionContext);
            WorkflowState = new() { Id = WorkflowExecutionContext.Id };
            WorkflowInstance = new() { Id = WorkflowExecutionContext.Id };
            NotificationBuffer = CreateBuffer(Mediator);
            Handler = new(
                WorkflowInstanceManager,
                BookmarkPersister,
                VariablePersistenceManager,
                Transaction,
                NotificationBuffer,
                NotificationSender,
                ActivityExecutionLogSink,
                WorkflowExecutionLogSink);
        }

        public WorkflowExecutionContext WorkflowExecutionContext { get; }
        public ActivityExecutionContext ActivityExecutionContext { get; }
        public WorkflowState WorkflowState { get; }
        public WorkflowInstance WorkflowInstance { get; }
        public IWorkflowInstanceManager WorkflowInstanceManager { get; } = Substitute.For<IWorkflowInstanceManager>();
        public IBookmarksPersister BookmarkPersister { get; } = Substitute.For<IBookmarksPersister>();
        public IVariablePersistenceManager VariablePersistenceManager { get; } = Substitute.For<IVariablePersistenceManager>();
        public IMediator Mediator { get; } = Substitute.For<IMediator>();
        public WorkflowCommitNotificationBuffer NotificationBuffer { get; }
        public INotificationSender NotificationSender { get; } = Substitute.For<INotificationSender>();
        public ILogRecordSink<ActivityExecutionRecord> ActivityExecutionLogSink { get; } = Substitute.For<ILogRecordSink<ActivityExecutionRecord>>();
        public ILogRecordSink<WorkflowExecutionLogRecord> WorkflowExecutionLogSink { get; } = Substitute.For<ILogRecordSink<WorkflowExecutionLogRecord>>();
        public RecordingWorkflowCommitTransaction Transaction { get; } = new();
        public DefaultCommitStateHandler Handler { get; }

        public static async Task<CommitTestFixture> CreateAsync()
        {
            var fixture = new ActivityTestFixture(new WriteLine("Test"));
            var activityExecutionContext = await fixture.BuildAsync();
            return new(activityExecutionContext);
        }

        public ValueTask DisposeAsync()
        {
            ((IDisposable)ActivityExecutionContext).Dispose();
            return ((IAsyncDisposable)WorkflowExecutionContext.ServiceProvider).DisposeAsync();
        }
    }

    private static WorkflowCommitNotificationBuffer CreateBuffer(IMediator mediator) => new(mediator, Substitute.For<ILogger<WorkflowCommitNotificationBuffer>>());

    private class TestNotification : INotification;

    private class RecordingWorkflowCommitTransaction : IWorkflowCommitTransaction
    {
        public bool IsExecuting { get; private set; }
        public bool Executed { get; private set; }
        public bool Completed { get; private set; }

        public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
        {
            Executed = true;
            IsExecuting = true;
            try
            {
                await operation(cancellationToken);
                Completed = true;
            }
            finally
            {
                IsExecuting = false;
            }
        }
    }
}
