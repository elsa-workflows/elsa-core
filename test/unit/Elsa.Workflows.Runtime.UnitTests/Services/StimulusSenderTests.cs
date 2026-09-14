using Elsa.Workflows;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class StimulusSenderTests
{
    private const string ActivityTypeName = "Elsa.RunTask";
    private const string StimulusHash = "stimulus-hash";
    private const string ActivityInstanceId = "activity-instance-1";
    private const string WorkflowInstanceId = "workflow-instance-1";
    private const string BookmarkId = "bookmark-1";
    private const string CorrelationId = "correlation-1";

    private readonly IStimulusHasher _stimulusHasher = Substitute.For<IStimulusHasher>();
    private readonly ITriggerBoundWorkflowService _triggerBoundWorkflowService = Substitute.For<ITriggerBoundWorkflowService>();
    private readonly IWorkflowResumer _workflowResumer = Substitute.For<IWorkflowResumer>();
    private readonly IBookmarkQueue _bookmarkQueue = Substitute.For<IBookmarkQueue>();
    private readonly ITriggerInvoker _triggerInvoker = Substitute.For<ITriggerInvoker>();

    public StimulusSenderTests()
    {
        _stimulusHasher.Hash(ActivityTypeName, Arg.Any<object>(), ActivityInstanceId).Returns(StimulusHash);
        _workflowResumer
            .ResumeAsync(Arg.Any<BookmarkFilter>(), Arg.Any<ResumeBookmarkOptions>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _triggerBoundWorkflowService
            .FindManyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    [Fact]
    public async Task SendAsync_WhenUnmatchedResume_EnqueuesActivityInstanceIdAndActivityTypeName()
    {
        var sender = CreateSender();
        var metadata = CreateMetadata();
        NewBookmarkQueueItem? enqueued = null;
        await _bookmarkQueue.EnqueueAsync(Arg.Do<NewBookmarkQueueItem>(item => enqueued = item), Arg.Any<CancellationToken>());

        await sender.SendAsync(ActivityTypeName, new object(), metadata);

        Assert.NotNull(enqueued);
        Assert.Equal(ActivityInstanceId, enqueued.ActivityInstanceId);
        Assert.Equal(ActivityTypeName, enqueued.ActivityTypeName);
        Assert.Equal(WorkflowInstanceId, enqueued.WorkflowInstanceId);
        Assert.Equal(BookmarkId, enqueued.BookmarkId);
        Assert.Equal(CorrelationId, enqueued.CorrelationId);
        Assert.Equal(StimulusHash, enqueued.StimulusHash);

        var filter = CreateBookmarkFilter(enqueued);
        Assert.Equal(ActivityInstanceId, filter.ActivityInstanceId);
        Assert.Equal(ActivityTypeName, filter.Name);
        Assert.Equal(StimulusHash, filter.Hash);
    }

    [Fact]
    public async Task SendAsync_WhenUnmatchedResumeWithHashOnly_EnqueuesActivityInstanceIdWithoutActivityTypeName()
    {
        var sender = CreateSender();
        var metadata = CreateMetadata();
        NewBookmarkQueueItem? enqueued = null;
        await _bookmarkQueue.EnqueueAsync(Arg.Do<NewBookmarkQueueItem>(item => enqueued = item), Arg.Any<CancellationToken>());

        await sender.SendAsync(StimulusHash, metadata);

        Assert.NotNull(enqueued);
        Assert.Equal(ActivityInstanceId, enqueued.ActivityInstanceId);
        Assert.Null(enqueued.ActivityTypeName);

        var filter = CreateBookmarkFilter(enqueued);
        Assert.Equal(ActivityInstanceId, filter.ActivityInstanceId);
        Assert.Null(filter.Name);
    }

    [Fact]
    public async Task SendAsync_WhenResumeMatches_DoesNotEnqueue()
    {
        _workflowResumer
            .ResumeAsync(Arg.Any<BookmarkFilter>(), Arg.Any<ResumeBookmarkOptions>(), Arg.Any<CancellationToken>())
            .Returns([new RunWorkflowInstanceResponse { WorkflowInstanceId = WorkflowInstanceId }]);
        var sender = CreateSender();

        await sender.SendAsync(ActivityTypeName, new object(), CreateMetadata());

        await _bookmarkQueue.DidNotReceive().EnqueueAsync(Arg.Any<NewBookmarkQueueItem>(), Arg.Any<CancellationToken>());
    }

    private StimulusSender CreateSender()
    {
        return new(
            _stimulusHasher,
            _triggerBoundWorkflowService,
            _workflowResumer,
            _bookmarkQueue,
            _triggerInvoker,
            NullLogger<StimulusSender>.Instance);
    }

    private static StimulusMetadata CreateMetadata()
    {
        return new()
        {
            WorkflowInstanceId = WorkflowInstanceId,
            ActivityInstanceId = ActivityInstanceId,
            BookmarkId = BookmarkId,
            CorrelationId = CorrelationId
        };
    }

    private static BookmarkFilter CreateBookmarkFilter(NewBookmarkQueueItem item)
    {
        return new BookmarkQueueItem
        {
            WorkflowInstanceId = item.WorkflowInstanceId,
            BookmarkId = item.BookmarkId,
            CorrelationId = item.CorrelationId,
            StimulusHash = item.StimulusHash,
            ActivityInstanceId = item.ActivityInstanceId,
            ActivityTypeName = item.ActivityTypeName,
            Options = item.Options
        }.CreateBookmarkFilter();
    }
}
