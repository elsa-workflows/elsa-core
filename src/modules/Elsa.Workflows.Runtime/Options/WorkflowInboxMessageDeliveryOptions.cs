using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Options for delivering a workflow inbox message.
/// </summary>
public class WorkflowInboxMessageDeliveryOptions
{
    /// <summary>
    /// The strategy to use when publishing the <see cref="WorkflowInboxMessageReceived"/> notification.
    /// </summary>
    public IEventPublishingStrategy EventPublishingStrategy { get; set; } = NotificationStrategy.Background;
}