using Elsa.Models;
using Elsa.Persistence.Entities;

namespace Elsa.Workflows.Runtime.Models;

public record IndexedWorkflowTriggers(Workflow Workflow, ICollection<WorkflowTrigger> AddedTriggers, ICollection<WorkflowTrigger> RemovedTriggers, ICollection<WorkflowTrigger> UnchangedTriggers);