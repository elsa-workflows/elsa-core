using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class EventPublisher(INotificationSender notificationSender) : IEventPublisher
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
        var eventNotification = new EventNotification(eventName, correlationId, workflowInstanceId, activityInstanceId, payload, asynchronous);
        await notificationSender.SendAsync(eventNotification, cancellationToken);
    }
}