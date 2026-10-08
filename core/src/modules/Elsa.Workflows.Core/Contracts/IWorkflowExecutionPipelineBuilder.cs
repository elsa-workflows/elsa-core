using Elsa.Workflows.Pipelines.WorkflowExecution;

namespace Elsa.Workflows;

/// <summary>
/// Builds a workflow execution pipeline.
/// </summary>
public interface IWorkflowExecutionPipelineBuilder
{
    /// <summary>
    /// A general-purpose dictionary of values that can be used by middleware components.
    /// </summary>
    public IDictionary<object, object?> Properties { get; }
    
    /// <summary>
    /// The middleware components that have been installed.
    /// </summary>
    public IEnumerable<Func<WorkflowMiddlewareDelegate, WorkflowMiddlewareDelegate>> Components { get; }
    
    /// <summary>
    /// The current service provider to resolve services from.
    /// </summary>
    IServiceProvider ServiceProvider { get; }
    
    /// <summary>
    /// Installs the specified delegate as a middleware component.
    /// </summary>
    IWorkflowExecutionPipelineBuilder Use(Func<WorkflowMiddlewareDelegate, WorkflowMiddlewareDelegate> middleware);
    
    /// <summary>
    /// Constructs the final <see cref="WorkflowMiddlewareDelegate"/> delegate that invokes each installed middleware component.
    /// </summary>
    public WorkflowMiddlewareDelegate Build();
    
    /// <summary>
    /// Clears the current pipeline.
    /// </summary>
    IWorkflowExecutionPipelineBuilder Reset();
    
    /// <summary>
    /// Inserts the middleware component at the specified index.
    /// </summary>
    IWorkflowExecutionPipelineBuilder Insert(int index, Func<WorkflowMiddlewareDelegate, WorkflowMiddlewareDelegate> middleware);

    /// <summary>
    /// Replaces the middleware component at the specified index with the specified delegate.
    /// </summary>
    /// <param name="index">The index of the middleware component to replace.</param>
    /// <param name="middleware">The delegate to use as the new middleware component.</param>
    /// <returns>A new instance of <see cref="IWorkflowExecutionPipelineBuilder"/> with the middleware component replaced.</returns>
    IWorkflowExecutionPipelineBuilder Replace(int index, Func<WorkflowMiddlewareDelegate, WorkflowMiddlewareDelegate> middleware);
}