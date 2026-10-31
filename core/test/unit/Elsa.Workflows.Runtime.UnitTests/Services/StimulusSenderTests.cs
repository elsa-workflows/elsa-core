using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
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

    [Fact]
    public async Task SendAsync_WhenStartingFromStoredTriggers_CarriesEachMatchedTriggersHashInACopyOfTheProperties()
    {
        var callerProperties = new Dictionary<string, object> { ["Caller"] = "value" };
        _stimulusHasher.Hash(ActivityTypeName, Arg.Any<object>(), null).Returns(StimulusHash);
        var firstTrigger = CreateTrigger("activity-1");
        var secondTrigger = CreateTrigger("activity-2");
        _triggerBoundWorkflowService
            .FindManyAsync(StimulusHash, Arg.Any<CancellationToken>())
            .Returns([new TriggerBoundWorkflow(CreateWorkflowGraph(), [firstTrigger, secondTrigger])]);
        var requests = new List<InvokeTriggerRequest>();
        _triggerInvoker
            .InvokeAsync(Arg.Do<InvokeTriggerRequest>(requests.Add), Arg.Any<CancellationToken>())
            .Returns(new StartWorkflowResponse { WorkflowInstanceId = WorkflowInstanceId, Status = WorkflowStatus.Running, SubStatus = WorkflowSubStatus.Suspended });
        var sender = CreateSender();

        await sender.SendAsync(ActivityTypeName, new object(), new StimulusMetadata { Properties = callerProperties });

        Assert.Equal(2, requests.Count);
        Assert.All(requests, request =>
        {
            Assert.NotNull(request.Properties);
            Assert.NotSame(callerProperties, request.Properties);
            Assert.Equal(StimulusHash, request.Properties[TriggerWorkflowProperties.StimulusHash]);
            Assert.Equal("value", request.Properties["Caller"]);
        });
        Assert.NotSame(requests[0].Properties, requests[1].Properties);
        Assert.False(callerProperties.ContainsKey(TriggerWorkflowProperties.StimulusHash));
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

    private static StoredTrigger CreateTrigger(string activityId) => new()
    {
        Id = $"trigger-{activityId}",
        WorkflowDefinitionId = "workflow",
        WorkflowDefinitionVersionId = "workflow-version",
        Name = ActivityTypeName,
        ActivityId = activityId,
        Hash = StimulusHash
    };

    private static WorkflowGraph CreateWorkflowGraph()
    {
        var workflow = new Workflow { Id = "workflow" };
        var root = new ActivityNode(workflow, "Root");
        return new(workflow, root, [root]);
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
