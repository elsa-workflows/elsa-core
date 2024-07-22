using Elsa.Workflows.Pipelines.WorkflowExecution;

namespace Elsa.Workflows;

/// Builds a workflow execution pipeline.
public interface IWorkflowExecutionPipelineBuilder
{
    /// A general-purpose dictionary of values that can be used by middleware components.
    public IDictionary<object, object?> Properties { get; }
    
    /// The middleware components that have been installed.
    public IEnumerable<Func<WorkflowMiddlewareDelegate, WorkflowMiddlewareDelegate>> Components { get; }
    
    /// The current service provider to resolve services from.
    IServiceProvider ServiceProvider { get; }
    
    /// Installs the specified delegate as a middleware component.
    IWorkflowExecutionPipelineBuilder Use(Func<WorkflowMiddlewareDelegate, WorkflowMiddlewareDelegate> middleware);
    
    /// Constructs the final <see cref="WorkflowMiddlewareDelegate"/> delegate that invokes each installed middleware component.
    public WorkflowMiddlewareDelegate Build();
    
    /// Clears the current pipeline.
    IWorkflowExecutionPipelineBuilder Reset();
    
    /// Inserts the middleware component at the specified index.
    IWorkflowExecutionPipelineBuilder Insert(int index, Func<WorkflowMiddlewareDelegate, WorkflowMiddlewareDelegate> middleware);

    /// <summary>
    /// Replaces the middleware component at the specified index with the specified delegate.
    /// </summary>
    /// <param name="index">The index of the middleware component to replace.</param>
    /// <param name="middleware">The delegate to use as the new middleware component.</param>
    /// <returns>A new instance of <see cref="IWorkflowExecutionPipelineBuilder"/> with the middleware component replaced.</returns>
    IWorkflowExecutionPipelineBuilder Replace(int index, Func<WorkflowMiddlewareDelegate, WorkflowMiddlewareDelegate> middleware);
}