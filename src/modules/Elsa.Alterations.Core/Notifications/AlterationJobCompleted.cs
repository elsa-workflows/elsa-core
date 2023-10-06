using Elsa.Alterations.Core.Entities;
using Elsa.Mediator.Contracts;

namespace Elsa.Alterations.Core.Notifications;

/// <summary>
/// A notification that is published when an alteration job is completed.
/// </summary>
public record AlterationJobCompleted(AlterationJob Job, bool WorkflowContainsScheduledWork) : INotification;