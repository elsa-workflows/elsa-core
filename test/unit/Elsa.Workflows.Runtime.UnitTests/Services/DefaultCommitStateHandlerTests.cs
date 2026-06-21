using Elsa.Mediator.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.State;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class DefaultCommitStateHandlerTests
{
    [Fact]
    public async Task CommitAsync_ExecutesPersistenceInsideCommitTransaction()
    {
        var (workflowExecutionContext, dirtyActivityExecutionContext) = await CreateWorkflowExecutionContextAsync();
        dirtyActivityExecutionContext.Taint();
        var workflowState = new WorkflowState { Id = workflowExecutionContext.Id };
        var workflowInstance = new WorkflowInstance { Id = workflowExecutionContext.Id };
        var workflowInstanceManager = Substitute.For<IWorkflowInstanceManager>();
        var bookmarkPersister = Substitute.For<IBookmarksPersister>();
        var variablePersistenceManager = Substitute.For<IVariablePersistenceManager>();
        var mediator = Substitute.For<IMediator>();
        var notificationBuffer = new WorkflowCommitNotificationBuffer(mediator);
        var notificationSender = Substitute.For<INotificationSender>();
        var activityExecutionLogSink = Substitute.For<ILogRecordSink<ActivityExecutionRecord>>();
        var workflowExecutionLogSink = Substitute.For<ILogRecordSink<WorkflowExecutionLogRecord>>();
        var transaction = new RecordingWorkflowCommitTransaction();
        workflowInstanceManager.SaveAsync(workflowState, Arg.Any<CancellationToken>()).Returns(workflowInstance);
        bookmarkPersister.When(x => x.PersistBookmarksAsync(Arg.Any<UpdateBookmarksRequest>())).Do(_ => Assert.True(transaction.IsExecuting));
        activityExecutionLogSink.When(x => x.PersistExecutionLogsAsync(workflowExecutionContext, Arg.Any<CancellationToken>())).Do(_ => Assert.True(transaction.IsExecuting));
        workflowExecutionLogSink.When(x => x.PersistExecutionLogsAsync(workflowExecutionContext, Arg.Any<CancellationToken>())).Do(_ => Assert.True(transaction.IsExecuting));
        variablePersistenceManager.When(x => x.SaveVariablesAsync(workflowExecutionContext)).Do(_ => Assert.True(transaction.IsExecuting));
        workflowInstanceManager.When(x => x.SaveAsync(workflowState, Arg.Any<CancellationToken>())).Do(_ => Assert.True(transaction.IsExecuting));
        var handler = new DefaultCommitStateHandler(
            workflowInstanceManager,
            bookmarkPersister,
            variablePersistenceManager,
            transaction,
            notificationBuffer,
            notificationSender,
            activityExecutionLogSink,
            workflowExecutionLogSink);

        await handler.CommitAsync(workflowExecutionContext, workflowState);

        Assert.True(transaction.Completed);
        Assert.False(dirtyActivityExecutionContext.IsDirty);
        await notificationSender.Received(1).SendAsync(
            Arg.Is<WorkflowStateCommitted>(x => x.WorkflowInstance == workflowInstance && x.WorkflowState == workflowState),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CommitAsync_WhenPersistenceFails_DoesNotClearExecutionLogOrPublishCommittedNotification()
    {
        var (workflowExecutionContext, dirtyActivityExecutionContext) = await CreateWorkflowExecutionContextAsync();
        dirtyActivityExecutionContext.Taint();
        workflowExecutionContext.AddExecutionLogEntry("Started");
        var workflowState = new WorkflowState { Id = workflowExecutionContext.Id };
        var workflowInstanceManager = Substitute.For<IWorkflowInstanceManager>();
        var bookmarkPersister = Substitute.For<IBookmarksPersister>();
        var variablePersistenceManager = Substitute.For<IVariablePersistenceManager>();
        var mediator = Substitute.For<IMediator>();
        var notificationBuffer = new WorkflowCommitNotificationBuffer(mediator);
        var notificationSender = Substitute.For<INotificationSender>();
        var activityExecutionLogSink = Substitute.For<ILogRecordSink<ActivityExecutionRecord>>();
        var workflowExecutionLogSink = Substitute.For<ILogRecordSink<WorkflowExecutionLogRecord>>();
        var transaction = new RecordingWorkflowCommitTransaction();
        workflowInstanceManager.SaveAsync(workflowState, Arg.Any<CancellationToken>()).Returns<Task<WorkflowInstance>>(_ => throw new InvalidOperationException("state save failed"));
        var handler = new DefaultCommitStateHandler(
            workflowInstanceManager,
            bookmarkPersister,
            variablePersistenceManager,
            transaction,
            notificationBuffer,
            notificationSender,
            activityExecutionLogSink,
            workflowExecutionLogSink);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.CommitAsync(workflowExecutionContext, workflowState));

        Assert.True(transaction.Executed);
        Assert.False(transaction.Completed);
        Assert.True(dirtyActivityExecutionContext.IsDirty);
        Assert.NotEmpty(workflowExecutionContext.ExecutionLog);
        await notificationSender.DidNotReceive().SendAsync(Arg.Any<WorkflowStateCommitted>(), Arg.Any<CancellationToken>());
    }

    private static async Task<(WorkflowExecutionContext WorkflowExecutionContext, ActivityExecutionContext DirtyActivityExecutionContext)> CreateWorkflowExecutionContextAsync()
    {
        var fixture = new ActivityTestFixture(new WriteLine("Test"));
        var activityExecutionContext = await fixture.BuildAsync();
        activityExecutionContext.Taint();
        var workflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;
        workflowExecutionContext.AddActivityExecutionContext(activityExecutionContext);
        return (workflowExecutionContext, activityExecutionContext);
    }

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
