using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Http;

/// <summary>
/// Represents the result of a workflow lookup.
/// </summary>
public record HttpWorkflowLookupResult(WorkflowGraph? WorkflowGraph, ICollection<StoredTrigger> Triggers);