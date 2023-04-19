using Elsa.Workflows.Core.Pipelines.WorkflowExecution;

namespace Elsa.Workflows.Core.Contracts;

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
}