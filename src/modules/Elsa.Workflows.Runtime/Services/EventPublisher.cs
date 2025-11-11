using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Stimuli;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class EventPublisher(IStimulusSender stimulusSender, IStimulusDispatcher stimulusDispatcher) : IEventPublisher
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
        var metadata = new StimulusMetadata
        {
            CorrelationId = correlationId,
            ActivityInstanceId = activityInstanceId,
            WorkflowInstanceId = workflowInstanceId,
            Input = workflowInput
        };
        var triggerName = ActivityTypeNameHelper.GenerateTypeName<Event>();
        if (asynchronous)
        {
            await stimulusDispatcher.SendAsync(new()
            {
                ActivityTypeName = triggerName,
                Stimulus = stimulus,
                Metadata = metadata
            }, cancellationToken);
        }
        else
            await stimulusSender.SendAsync(triggerName, stimulus, metadata, cancellationToken);
    }
}