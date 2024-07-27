using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Stimuli;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class EventPublisher(IStimulusSender stimulusSender) : IEventPublisher
{
    /// <inheritdoc />
    public async Task PublishAsync(
        string eventName,
        string? correlationId = default,
        string? workflowInstanceId = default,
        string? activityInstanceId = default,
        object? payload = default,
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
        await stimulusSender.SendAsync<Event>(stimulus, metadata, cancellationToken);
    }
}