using Elsa.Models;
using Elsa.Persistence.Entities;

namespace Elsa.Runtime.Models;

public record IndexedWorkflow(Workflow Workflow, ICollection<WorkflowTrigger> AddedTriggers, ICollection<WorkflowTrigger> RemovedTriggers);