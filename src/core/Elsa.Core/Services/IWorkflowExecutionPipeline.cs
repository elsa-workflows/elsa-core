using Elsa.Models;
using Elsa.Pipelines.WorkflowExecution;

namespace Elsa.Services;

public interface IWorkflowExecutionPipeline
{
    WorkflowMiddlewareDelegate Setup(Action<IWorkflowExecutionBuilder> setup);
    WorkflowMiddlewareDelegate Pipeline { get; }
    Task ExecuteAsync(WorkflowExecutionContext context);
}