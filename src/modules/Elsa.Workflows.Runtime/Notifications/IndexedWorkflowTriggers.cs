using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// Represents a collection of indexed workflow triggers.
/// </summary>
public record IndexedWorkflowTriggers(Workflow Workflow, ICollection<StoredTrigger> AddedTriggers, ICollection<StoredTrigger> RemovedTriggers, ICollection<StoredTrigger> UnchangedTriggers);