using Elsa.Workflows;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Handlers;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Results;
using Elsa.Workflows.Runtime.Stimuli;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Handlers;

public class ResumeBulkDispatchWorkflowActivityTests
{
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
}
