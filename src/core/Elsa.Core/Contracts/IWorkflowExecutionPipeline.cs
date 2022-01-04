using Elsa.Models;
using Elsa.Pipelines.WorkflowExecution;

namespace Elsa.Contracts;

public interface IWorkflowExecutionPipeline
{
    WorkflowMiddlewareDelegate Setup(Action<IWorkflowExecutionBuilder> setup);
    WorkflowMiddlewareDelegate Pipeline { get; }
    Task ExecuteAsync(WorkflowExecutionContext context);
}