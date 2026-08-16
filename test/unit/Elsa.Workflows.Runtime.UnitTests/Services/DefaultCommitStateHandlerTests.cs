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
    [Fact]
    public async Task CommitAsync_ExecutesPersistenceInsideCommitTransaction()
    {
        var fixture = await CommitTestFixture.CreateAsync();
        fixture.WorkflowInstanceManager.SaveAsync(fixture.WorkflowState, Arg.Any<CancellationToken>()).Returns(fixture.WorkflowInstance);
        fixture.BookmarkPersister.When(x => x.PersistBookmarksAsync(Arg.Any<UpdateBookmarksRequest>())).Do(_ => Assert.True(fixture.Transaction.IsExecuting));
        fixture.ActivityExecutionLogSink.When(x => x.PersistExecutionLogsAsync(fixture.WorkflowExecutionContext, Arg.Any<CancellationToken>())).Do(_ => Assert.True(fixture.Transaction.IsExecuting));
        fixture.WorkflowExecutionLogSink.When(x => x.PersistExecutionLogsAsync(fixture.WorkflowExecutionContext, Arg.Any<CancellationToken>())).Do(_ => Assert.True(fixture.Transaction.IsExecuting));
        fixture.VariablePersistenceManager.When(x => x.SaveVariablesAsync(fixture.WorkflowExecutionContext)).Do(_ => Assert.True(fixture.Transaction.IsExecuting));
        fixture.WorkflowInstanceManager.When(x => x.SaveAsync(fixture.WorkflowState, Arg.Any<CancellationToken>())).Do(_ => Assert.True(fixture.Transaction.IsExecuting));

        await fixture.Handler.CommitAsync(fixture.WorkflowExecutionContext, fixture.WorkflowState);

        Assert.True(fixture.Transaction.Completed);
        Assert.False(fixture.ActivityExecutionContext.IsDirty);
        await fixture.NotificationSender.Received(1).SendAsync(
            Arg.Is<WorkflowStateCommitted>(x => x.WorkflowInstance == fixture.WorkflowInstance && x.WorkflowState == fixture.WorkflowState),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CommitAsync_WhenPersistenceFails_DoesNotClearExecutionLogOrPublishCommittedNotification()
    {
        var fixture = await CommitTestFixture.CreateAsync();
        fixture.WorkflowExecutionContext.AddExecutionLogEntry("Started");
        fixture.WorkflowInstanceManager.SaveAsync(fixture.WorkflowState, Arg.Any<CancellationToken>()).Returns<Task<WorkflowInstance>>(_ => throw new InvalidOperationException("state save failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Handler.CommitAsync(fixture.WorkflowExecutionContext, fixture.WorkflowState));

        Assert.True(fixture.Transaction.Executed);
        Assert.False(fixture.Transaction.Completed);
        Assert.True(fixture.ActivityExecutionContext.IsDirty);
        Assert.NotEmpty(fixture.WorkflowExecutionContext.ExecutionLog);
        await fixture.NotificationSender.DidNotReceive().SendAsync(Arg.Any<WorkflowStateCommitted>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CommitAsync_WhenBufferedNotificationFlushFails_PublishesCommittedNotification()
    {
        var fixture = await CommitTestFixture.CreateAsync();
        var bufferedNotificationSender = new WorkflowCommitNotificationSender(fixture.Mediator, fixture.NotificationBuffer);
        var bufferedNotification = new TestNotification();
        fixture.WorkflowInstanceManager.SaveAsync(fixture.WorkflowState, Arg.Any<CancellationToken>()).Returns(fixture.WorkflowInstance);
        fixture.ActivityExecutionLogSink
            .PersistExecutionLogsAsync(fixture.WorkflowExecutionContext, Arg.Any<CancellationToken>())
            .Returns(_ => bufferedNotificationSender.SendAsync(bufferedNotification));
        fixture.Mediator
            .SendAsync(bufferedNotification, Arg.Any<IEventPublishingStrategy?>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("Buffered handler failed"));

        await Assert.ThrowsAsync<AggregateException>(() => fixture.Handler.CommitAsync(fixture.WorkflowExecutionContext, fixture.WorkflowState));

        await fixture.NotificationSender.Received(1).SendAsync(
            Arg.Is<WorkflowStateCommitted>(x => x.WorkflowInstance == fixture.WorkflowInstance && x.WorkflowState == fixture.WorkflowState),
            Arg.Any<CancellationToken>());
    }

    private class CommitTestFixture
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
