using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Workflows.Runtime.Results;

public record DeliverWorkflowInboxMessageResult(ICollection<WorkflowExecutionResult> WorkflowExecutionResults);