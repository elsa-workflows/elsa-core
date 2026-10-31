using Elsa.Workflows;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Handlers;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Results;
using Elsa.Workflows.Runtime.Stimuli;
using Elsa.Workflows.State;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Handlers;

public class ResumeBulkDispatchWorkflowActivityTests
{
    [Fact]
    public async Task HandleAsync_WaitedForChildCancelled_QueuesResumeOfTheWaitingActivityWithCancelledSubStatus()
    {
        var bookmarkQueue = Substitute.For<IBookmarkQueue>();
        NewBookmarkQueueItem? enqueuedItem = null;
        await bookmarkQueue.EnqueueAsync(Arg.Do<NewBookmarkQueueItem>(item => enqueuedItem = item), Arg.Any<CancellationToken>());
        var stimulusHasher = Substitute.For<IStimulusHasher>();
        stimulusHasher.Hash(Arg.Any<string>(), Arg.Is<BulkDispatchWorkflowsStimulus>(stimulus => stimulus.ParentInstanceId == "parent-1"), Arg.Any<string?>()).Returns("parent-1-hash");
        var handler = new ResumeBulkDispatchWorkflowActivity(bookmarkQueue, stimulusHasher, Substitute.For<IStimulusSender>());

        await handler.HandleAsync(new WorkflowCancelled("child-1") { WorkflowState = CancelledWaitedForChild("child-1", "parent-1", "bulk-activity-1") }, CancellationToken.None);

        Assert.NotNull(enqueuedItem);
        Assert.Equal("parent-1", enqueuedItem.WorkflowInstanceId);
        Assert.Equal("parent-1-hash", enqueuedItem.StimulusHash);
        Assert.Equal("bulk-activity-1", enqueuedItem.ActivityInstanceId);
        var input = Assert.IsType<Dictionary<string, object>>(enqueuedItem.Options!.Input);
        Assert.Equal("child-1", input["WorkflowInstanceId"]);
        Assert.Equal(WorkflowStatus.Finished, input["WorkflowStatus"]);
        Assert.Equal(WorkflowSubStatus.Cancelled, input["WorkflowSubStatus"]);
        var output = Assert.IsType<Dictionary<string, object>>(input["WorkflowOutput"]);
        Assert.Equal("shipped", output["Status"]);
    }

    [Fact]
    public async Task HandleAsync_CancellingAnAlreadyFinishedChild_DoesNotResumeTheParentAgain()
    {
        var bookmarkQueue = Substitute.For<IBookmarkQueue>();
        var handler = new ResumeBulkDispatchWorkflowActivity(bookmarkQueue, Substitute.For<IStimulusHasher>(), Substitute.For<IStimulusSender>());
        var finishedChild = CancelledWaitedForChild("child-1", "parent-1", "bulk-activity-1");
        finishedChild.SubStatus = WorkflowSubStatus.Finished;

        await handler.HandleAsync(new WorkflowCancelled("child-1") { WorkflowState = finishedChild }, CancellationToken.None);

        await bookmarkQueue.DidNotReceive().EnqueueAsync(Arg.Any<NewBookmarkQueueItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CancelledChildThatNoBulkDispatchWaitsFor_DoesNotResumeTheParent()
    {
        var bookmarkQueue = Substitute.For<IBookmarkQueue>();
        var handler = new ResumeBulkDispatchWorkflowActivity(bookmarkQueue, Substitute.For<IStimulusHasher>(), Substitute.For<IStimulusSender>());
        var dispatchedChild = CancelledWaitedForChild("child-1", "parent-1", "bulk-activity-1");
        dispatchedChild.Properties.Remove(WorkflowDispatchProperties.WaitingActivityInstanceId);

        await handler.HandleAsync(new WorkflowCancelled("child-1") { WorkflowState = dispatchedChild }, CancellationToken.None);

        await bookmarkQueue.DidNotReceive().EnqueueAsync(Arg.Any<NewBookmarkQueueItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ActivationDenied_RoutesFaultedChildCompletionToParentBookmark()
    {
        var bookmarkQueue = Substitute.For<IBookmarkQueue>();
        var stimulusHasher = Substitute.For<IStimulusHasher>();
        var stimulusSender = Substitute.For<IStimulusSender>();
        StimulusMetadata? observedMetadata = null;
        stimulusSender.SendAsync(Arg.Any<string>(), Arg.Any<StimulusMetadata>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                observedMetadata = call.Arg<StimulusMetadata>();
                return Task.FromResult(new SendStimulusResult([]));
            });
        var handler = new ResumeBulkDispatchWorkflowActivity(bookmarkQueue, stimulusHasher, stimulusSender);
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<BulkDispatchWorkflows>();

        await handler.HandleAsync(
            new DispatchWorkflowActivationDenied("parent-1", "child-1", activityTypeName, "bulk-stimulus-hash"),
            CancellationToken.None);

        await stimulusSender.Received(1).SendAsync(
            "bulk-stimulus-hash",
            Arg.Any<StimulusMetadata>(),
            Arg.Any<CancellationToken>());
        Assert.NotNull(observedMetadata);
        Assert.Equal("parent-1", observedMetadata.WorkflowInstanceId);
        var input = Assert.IsType<Dictionary<string, object>>(observedMetadata.Input);
        Assert.Equal(true, input["CannotStart"]);
        Assert.Equal("child-1", input["WorkflowInstanceId"]);
        Assert.Equal(WorkflowStatus.Finished, input["WorkflowStatus"]);
        Assert.Equal(WorkflowSubStatus.Faulted, input["WorkflowSubStatus"]);
        Assert.IsType<Dictionary<string, object>>(input["WorkflowOutput"]);
        await bookmarkQueue.DidNotReceive().EnqueueAsync(Arg.Any<NewBookmarkQueueItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ActivationDeniedForAnotherActivity_DoesNotRouteToParent()
    {
        var stimulusSender = Substitute.For<IStimulusSender>();
        var handler = new ResumeBulkDispatchWorkflowActivity(
            Substitute.For<IBookmarkQueue>(),
            Substitute.For<IStimulusHasher>(),
            stimulusSender);

        await handler.HandleAsync(
            new DispatchWorkflowActivationDenied("parent-1", "child-1", "Elsa.DispatchWorkflow", "single-stimulus-hash"),
            CancellationToken.None);

        await stimulusSender.DidNotReceive().SendAsync(
            Arg.Any<string>(),
            Arg.Any<StimulusMetadata>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>The state the canceler reports for a cancelled child a parent's <c>BulkDispatchWorkflows</c> was waiting for.</summary>
    private static WorkflowState CancelledWaitedForChild(string childId, string parentId, string waitingActivityInstanceId) => new()
    {
        Id = childId,
        Status = WorkflowStatus.Finished,
        SubStatus = WorkflowSubStatus.Cancelled,
        Output = new Dictionary<string, object> { ["Status"] = "shipped" },
        Properties = new Dictionary<string, object>
        {
            [WorkflowDispatchProperties.WaitForCompletion] = true,
            [WorkflowDispatchProperties.WaitingActivityInstanceId] = waitingActivityInstanceId,
            ["ParentInstanceId"] = parentId
        }
    };
}
