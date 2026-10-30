using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Stimuli;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class EventPublisherTests
{
    private readonly IStimulusSender _stimulusSender = Substitute.For<IStimulusSender>();
    private readonly IWorkflowDispatcher _workflowDispatcher = Substitute.For<IWorkflowDispatcher>();

    [Fact]
    public async Task PublishAsync_WhenAsynchronous_DispatchesThroughWorkflowDispatcher()
    {
        var publisher = CreatePublisher();
        const string eventName = "OrderShipped";
        const string correlationId = "corr-1";
        const string workflowInstanceId = "wf-1";
        const string activityInstanceId = "act-1";
        var payload = new { Status = "Shipped" };

        await publisher.PublishAsync(eventName, correlationId, workflowInstanceId, activityInstanceId, payload, asynchronous: true);

        await _workflowDispatcher.Received(1).DispatchAsync(
            Arg.Is<DispatchTriggerWorkflowsRequest>(request => IsMatchingTriggerRequest(request, eventName, correlationId, workflowInstanceId, activityInstanceId, payload)),
            Arg.Any<DispatchWorkflowOptions?>(),
            Arg.Any<CancellationToken>());
        await _stimulusSender.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task PublishAsync_WhenSynchronous_SendsThroughStimulusSender()
    {
        var publisher = CreatePublisher();
        const string eventName = "OrderShipped";
        var payload = new Dictionary<string, object> { ["Status"] = "Shipped" };

        await publisher.PublishAsync(eventName, payload: payload, asynchronous: false);

        await _stimulusSender.Received(1).SendAsync(
            ActivityTypeNameHelper.GenerateTypeName<Event>(),
            Arg.Is<EventStimulus>(stimulus => stimulus.EventName == eventName),
            Arg.Is<StimulusMetadata>(metadata =>
                metadata.Input != null &&
                metadata.Input[Event.EventInputWorkflowInputKey] == payload),
            Arg.Any<CancellationToken>());
        await _workflowDispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default(DispatchTriggerWorkflowsRequest)!, default, default);
    }

    private EventPublisher CreatePublisher() => new(_stimulusSender, _workflowDispatcher);

    private static bool IsMatchingTriggerRequest(
        DispatchTriggerWorkflowsRequest request,
        string eventName,
        string correlationId,
        string workflowInstanceId,
        string activityInstanceId,
        object payload)
    {
        var stimulus = request.BookmarkPayload as EventStimulus;
        return request.ActivityTypeName == ActivityTypeNameHelper.GenerateTypeName<Event>() &&
               stimulus != null &&
               stimulus.EventName == eventName &&
               request.CorrelationId == correlationId &&
               request.WorkflowInstanceId == workflowInstanceId &&
               request.ActivityInstanceId == activityInstanceId &&
               request.Input != null &&
               request.Input[Event.EventInputWorkflowInputKey] == payload;
    }
}
