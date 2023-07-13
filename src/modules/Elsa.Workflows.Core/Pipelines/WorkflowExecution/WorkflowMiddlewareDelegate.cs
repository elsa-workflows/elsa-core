namespace Elsa.Workflows.Core.Pipelines.WorkflowExecution;

public delegate ValueTask WorkflowMiddlewareDelegate(WorkflowExecutionContext context);