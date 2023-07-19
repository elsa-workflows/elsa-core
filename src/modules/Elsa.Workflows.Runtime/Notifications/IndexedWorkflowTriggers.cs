using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Notifications;

public record IndexedWorkflowTriggers(Workflow Workflow, ICollection<StoredTrigger> AddedTriggers, ICollection<StoredTrigger> RemovedTriggers, ICollection<StoredTrigger> UnchangedTriggers);