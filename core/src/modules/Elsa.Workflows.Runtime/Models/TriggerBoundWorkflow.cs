using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents a workflow bound to one or more triggers.
/// </summary>
public record TriggerBoundWorkflow(WorkflowGraph WorkflowGraph, ICollection<StoredTrigger> Triggers);