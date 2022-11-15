using System.Collections.Generic;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Models;

public record IndexedWorkflowTriggers(Workflow Workflow, ICollection<StoredTrigger> AddedTriggers, ICollection<StoredTrigger> RemovedTriggers, ICollection<StoredTrigger> UnchangedTriggers);