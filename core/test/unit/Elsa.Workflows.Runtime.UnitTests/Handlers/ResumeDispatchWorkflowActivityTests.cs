using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Handlers;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Results;
using Elsa.Workflows.Runtime.Stimuli;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Handlers;

public class ResumeDispatchWorkflowActivityTests
{
    [Fact]
    public async Task HandleAsync_ActivationDenied_RoutesParentBookmarkResumeWithDefinedOutcome()
    {
        var bookmarkQueue = Substitute.For<IBookmarkQueue>();
        var stimulusHasher = Substitute.For<IStimulusHasher>();
        var stimulusSender = Substitute.For<IStimulusSender>();
        var handler = new ResumeDispatchWorkflowActivity(bookmarkQueue, stimulusHasher, stimulusSender);
        StimulusMetadata? observedMetadata = null;
        stimulusSender.SendAsync(Arg.Any<string>(), Arg.Any<StimulusMetadata>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                observedMetadata = call.Arg<StimulusMetadata>();
                return Task.FromResult(new SendStimulusResult([]));
            });

        await handler.HandleAsync(new DispatchWorkflowActivationDenied("parent-1", "child-1", "Elsa.DispatchWorkflow", "dispatch-stimulus-hash"), CancellationToken.None);

        await stimulusSender.Received(1).SendAsync(
            "dispatch-stimulus-hash",
            Arg.Any<StimulusMetadata>(),
            Arg.Any<CancellationToken>());
        Assert.NotNull(observedMetadata);
        Assert.Equal("parent-1", observedMetadata.WorkflowInstanceId);
        var input = Assert.IsType<Dictionary<string, object>>(observedMetadata.Input);
        Assert.Equal(true, input["CannotStart"]);
        Assert.Equal("child-1", input["WorkflowInstanceId"]);
        stimulusHasher.DidNotReceive().Hash(Arg.Any<string>(), Arg.Any<DispatchWorkflowStimulus>());
        await bookmarkQueue.DidNotReceive().EnqueueAsync(Arg.Any<NewBookmarkQueueItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ActivationDeniedBeforeParentBookmark_QueuesResumeForExactParentRoute()
    {
        var bookmarkQueue = Substitute.For<IBookmarkQueue>();
        NewBookmarkQueueItem? enqueuedItem = null;
        await bookmarkQueue.EnqueueAsync(Arg.Do<NewBookmarkQueueItem>(item => enqueuedItem = item), Arg.Any<CancellationToken>());
        var workflowResumer = Substitute.For<IWorkflowResumer>();
        workflowResumer.ResumeAsync(Arg.Any<BookmarkFilter>(), Arg.Any<ResumeBookmarkOptions>(), Arg.Any<CancellationToken>()).Returns([]);
        var stimulusSender = new StimulusSender(
            Substitute.For<IStimulusHasher>(),
            Substitute.For<ITriggerBoundWorkflowService>(),
            workflowResumer,
            bookmarkQueue,
            Substitute.For<ITriggerInvoker>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<StimulusSender>.Instance);
        var handler = new ResumeDispatchWorkflowActivity(bookmarkQueue, Substitute.For<IStimulusHasher>(), stimulusSender);

        await handler.HandleAsync(
            new DispatchWorkflowActivationDenied("parent-1", "child-1", "Elsa.DispatchWorkflow", "dispatch-stimulus-hash"),
            CancellationToken.None);

        Assert.NotNull(enqueuedItem);
        Assert.Equal("parent-1", enqueuedItem.WorkflowInstanceId);
        Assert.Equal("dispatch-stimulus-hash", enqueuedItem.StimulusHash);
        var input = Assert.IsType<Dictionary<string, object>>(enqueuedItem.Options!.Input);
        Assert.Equal(true, input["CannotStart"]);
        Assert.Equal("child-1", input["WorkflowInstanceId"]);
        await workflowResumer.Received(1).ResumeAsync(
            Arg.Is<BookmarkFilter>(filter => filter.WorkflowInstanceId == "parent-1" && filter.Hash == "dispatch-stimulus-hash"),
            Arg.Any<ResumeBookmarkOptions>(),
            Arg.Any<CancellationToken>());
    }
}
