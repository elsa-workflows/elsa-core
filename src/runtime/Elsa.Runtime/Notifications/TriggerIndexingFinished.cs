using Elsa.Mediator.Contracts;
using Elsa.Persistence.Entities;

namespace Elsa.Runtime.Notifications;

public record TriggerIndexingFinished(ICollection<WorkflowTrigger> Triggers) : INotification;