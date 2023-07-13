using Elsa.Workflows.Core.Pipelines.WorkflowExecution;

namespace Elsa.Workflows.Core.Contracts;

public interface IWorkflowExecutionPipeline
{
    WorkflowMiddlewareDelegate Setup(Action<IWorkflowExecutionPipelineBuilder> setup);
    WorkflowMiddlewareDelegate Pipeline { get; }
    Task ExecuteAsync(WorkflowExecutionContext context);
}