using Elsa.Persistence.Entities;
using Elsa.Runtime.Contracts;

namespace Elsa.Runtime.Instructions;

public record TriggerWorkflowInstruction(WorkflowTrigger WorkflowTrigger, IReadOnlyDictionary<string, object>? Input) : IWorkflowInstruction;