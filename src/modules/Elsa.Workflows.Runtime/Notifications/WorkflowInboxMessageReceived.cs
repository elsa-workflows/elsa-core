using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// A notification that is sent when a workflow inbox message is received.
/// </summary>
/// <param name="InboxMessage">The inbox message that was received.</param>
public record WorkflowInboxMessageReceived(WorkflowInboxMessage InboxMessage) : INotification;