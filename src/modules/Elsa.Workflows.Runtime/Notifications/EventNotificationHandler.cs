using Elsa.Mediator.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Stimuli;

namespace Elsa.Workflows.Runtime.Notifications;

/// <inheritdoc />
public class EventNotificationHandler(IStimulusSender stimulusSender, IStimulusDispatcher stimulusDispatcher) : INotificationHandler<EventNotification>
{
    /// <inheritdoc />
    public async Task HandleAsync(EventNotification notification, CancellationToken cancellationToken)
    {
        string eventName = notification.EventName;
        string? correlationId = notification.CorrelationId;
        string? workflowInstanceId = notification.WorkflowInstanceId;
        string? activityInstanceId = notification.ActivityInstanceId;
        object? payload = notification.Payload;
        bool asynchronous = notification.Asynchronous;
            
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
        if (asynchronous)
        {
            await stimulusDispatcher.SendAsync(new()
            {
                ActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<Event>(),
                Stimulus = stimulus,
                Metadata = metadata
            }, cancellationToken);
        }
        else
            await stimulusSender.SendAsync<Event>(stimulus, metadata, cancellationToken);
    }
}