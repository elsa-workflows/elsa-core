using Elsa.Pipelines.WorkflowExecution;

namespace Elsa.Contracts;

public interface IWorkflowExecutionBuilder
{
    public IDictionary<string, object?> Properties { get; }
    IServiceProvider ApplicationServices { get; }
    IWorkflowExecutionBuilder Use(Func<WorkflowMiddlewareDelegate, WorkflowMiddlewareDelegate> middleware);
    public WorkflowMiddlewareDelegate Build();
}