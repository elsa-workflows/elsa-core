using Elsa.Workflows;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Testing.Shared.Models;

/// <summary>
/// Represents the result of a test workflow execution, including the workflow execution context and activity execution records.
/// </summary>
/// <param name="WorkflowExecutionContext">The workflow execution context after completion.</param>
/// <param name="ActivityExecutionRecords">The collection of activity execution records for the workflow.</param>
public record TestWorkflowExecutionResult(WorkflowExecutionContext WorkflowExecutionContext, ICollection<ActivityExecutionRecord> ActivityExecutionRecords);