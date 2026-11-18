using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Notifications;

// Cache observers use this signal after the trigger store has committed required removal.
internal record RequiredWorkflowTriggerRecordsRemoved(IReadOnlyCollection<StoredTrigger> Triggers) : INotification;
