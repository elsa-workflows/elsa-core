using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Stimuli;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class EventPublisher(IStimulusSender stimulusSender, IWorkflowDispatcher workflowDispatcher) : IEventPublisher
{
    /// <inheritdoc />
    public async Task PublishAsync(
        string eventName,
        string? correlationId = null,
        string? workflowInstanceId = null,
        string? activityInstanceId = null,
        object? payload = null,
        bool asynchronous = false,
        CancellationToken cancellationToken = default)
    {
        var stimulus = new EventStimulus(eventName);
        var workflowInput = new Dictionary<string, object>
        {
            [Event.EventInputWorkflowInputKey] = payload ?? new Dictionary<string, object>()
        };
        var triggerName = ActivityTypeNameHelper.GenerateTypeName<Event>();

        if (asynchronous)
        {
            await workflowDispatcher.DispatchAsync(new DispatchTriggerWorkflowsRequest(triggerName, stimulus)
            {
                CorrelationId = correlationId,
                WorkflowInstanceId = workflowInstanceId,
                ActivityInstanceId = activityInstanceId,
                Input = workflowInput
            }, options: null, cancellationToken);
            return;
        }

        var metadata = new StimulusMetadata
        {
            CorrelationId = correlationId,
            ActivityInstanceId = activityInstanceId,
            WorkflowInstanceId = workflowInstanceId,
            Input = workflowInput
        };
        await stimulusSender.SendAsync(triggerName, stimulus, metadata, cancellationToken);
    }
}