using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents a workflow bound to one or more triggers.
/// </summary>
public record TriggerBoundWorkflow(Workflow Workflow, ICollection<StoredTrigger> Triggers);