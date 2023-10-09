using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Results;

/// <summary>
/// Contains workflow execution results
/// </summary>
/// <param name="Message">The message that was submitted.</param>
/// <param name="WorkflowExecutionResults">Contains workflow execution results</param>
public record SubmitWorkflowInboxMessageResult(WorkflowInboxMessage Message, ICollection<WorkflowExecutionResult> WorkflowExecutionResults);