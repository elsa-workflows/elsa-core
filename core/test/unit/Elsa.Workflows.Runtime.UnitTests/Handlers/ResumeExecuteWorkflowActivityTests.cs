using Elsa.Workflows.Runtime.Handlers;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Stimuli;
using Elsa.Workflows.State;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Handlers;

public class ResumeExecuteWorkflowActivityTests
{
    [Fact]
    public async Task HandleAsync_WaitedForChildCancelled_QueuesParentResumeWithChildOutput()
    {
        var bookmarkQueue = Substitute.For<IBookmarkQueue>();
        NewBookmarkQueueItem? enqueuedItem = null;
        await bookmarkQueue.EnqueueAsync(Arg.Do<NewBookmarkQueueItem>(item => enqueuedItem = item), Arg.Any<CancellationToken>());
        var stimulusHasher = Substitute.For<IStimulusHasher>();
        stimulusHasher.Hash(Arg.Any<string>(), Arg.Is<ExecuteWorkflowStimulus>(stimulus => stimulus.ChildInstanceId == "child-1"), Arg.Any<string?>()).Returns("child-1-hash");
        var handler = new ResumeExecuteWorkflowActivity(bookmarkQueue, stimulusHasher);

        await handler.HandleAsync(new WorkflowCancelled("child-1") { WorkflowState = CancelledWaitedForChild("child-1", "parent-1") }, CancellationToken.None);

        Assert.NotNull(enqueuedItem);
        Assert.Equal("parent-1", enqueuedItem.WorkflowInstanceId);
        Assert.Equal("child-1-hash", enqueuedItem.StimulusHash);
        var input = Assert.IsType<Dictionary<string, object>>(enqueuedItem.Options!.Input);
        Assert.Equal("shipped", input["Status"]);
    }

    [Fact]
    public async Task HandleAsync_CancellingAnAlreadyFinishedChild_DoesNotResumeTheParentAgain()
    {
        var bookmarkQueue = Substitute.For<IBookmarkQueue>();
        var handler = new ResumeExecuteWorkflowActivity(bookmarkQueue, Substitute.For<IStimulusHasher>());
        var finishedChild = CancelledWaitedForChild("child-1", "parent-1");
        finishedChild.SubStatus = WorkflowSubStatus.Finished;

        await handler.HandleAsync(new WorkflowCancelled("child-1") { WorkflowState = finishedChild }, CancellationToken.None);

        await bookmarkQueue.DidNotReceive().EnqueueAsync(Arg.Any<NewBookmarkQueueItem>(), Arg.Any<CancellationToken>());
    }

    /// <summary>The state the canceler reports for a cancelled child its parent's <c>ExecuteWorkflow</c> was waiting for.</summary>
    private static WorkflowState CancelledWaitedForChild(string childId, string parentId) => new()
    {
        Id = childId,
        Status = WorkflowStatus.Finished,
        SubStatus = WorkflowSubStatus.Cancelled,
        Output = new Dictionary<string, object> { ["Status"] = "shipped" },
        Properties = new Dictionary<string, object>
        {
            [WorkflowDispatchProperties.WaitForCompletion] = true,
            ["ParentInstanceId"] = parentId
        }
    };
}
