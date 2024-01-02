using Elsa.Workflows.Pipelines.WorkflowExecution;

namespace Elsa.Workflows.Contracts;

public interface IWorkflowExecutionPipeline
{
    WorkflowMiddlewareDelegate Setup(Action<IWorkflowExecutionPipelineBuilder> setup);
    WorkflowMiddlewareDelegate Pipeline { get; }
    Task ExecuteAsync(WorkflowExecutionContext context);
}