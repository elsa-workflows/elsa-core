using Elsa.Mediator.Contracts;
using Elsa.Mqtt.Activities;
using Elsa.Mqtt.Notifications;
using Elsa.Mqtt.Utilities;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Options;
using JetBrains.Annotations;

namespace Elsa.Mqtt.Handlers;

/// <summary>
/// Handles <see cref="MqttMessageReceivedNotification"/> and invokes matching workflow triggers and bookmarks.
/// </summary>
[UsedImplicitly]
internal class TriggerMqttWorkflows(ITriggerInvoker triggerInvoker, IBookmarkQueue bookmarkQueue) :
    INotificationHandler<MqttMessageReceivedNotification>
{
    private static readonly string ActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<MqttMessageReceived>();

    public async Task HandleAsync(MqttMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        var subscriber = notification.Subscriber;
        var transportMessage = notification.TransportMessage;
        var topic = transportMessage.Topic;

        var input = new Dictionary<string, object>
        {
            [MqttMessageReceived.InputKey] = transportMessage
        };

        var matchingTriggers = subscriber.TriggerBindings.Values
            .Where(b => b.Stimulus.Topics.Any(filter => MqttTopicMatcher.IsMatch(filter, topic)))
            .ToList();

        var matchingBookmarks = subscriber.BookmarkBindings.Values
            .Where(b => b.Stimulus.Topics.Any(filter => MqttTopicMatcher.IsMatch(filter, topic)))
            .ToList();

        foreach (var binding in matchingTriggers)
        {
            await triggerInvoker.InvokeAsync(new InvokeTriggerRequest
            {
                Workflow = binding.Workflow,
                ActivityId = binding.TriggerActivityId,
                Input = input,
            }, cancellationToken);
        }

        foreach (var binding in matchingBookmarks)
        {
            await bookmarkQueue.EnqueueAsync(new NewBookmarkQueueItem
            {
                WorkflowInstanceId = binding.WorkflowInstanceId,
                BookmarkId = binding.BookmarkId,
                Options = new ResumeBookmarkOptions
                {
                    Input = input,
                },
                ActivityTypeName = ActivityTypeName,
            }, cancellationToken);
        }
    }
}
