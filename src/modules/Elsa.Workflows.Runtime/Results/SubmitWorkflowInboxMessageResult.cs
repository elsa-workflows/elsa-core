using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Workflows.Runtime.Results;

/// <summary>
/// Contains workflow execution results
/// </summary>
/// <param name="WorkflowExecutionResults">Contains workflow execution results</param>
public record SubmitWorkflowInboxMessageResult(ICollection<WorkflowExecutionResult> WorkflowExecutionResults);