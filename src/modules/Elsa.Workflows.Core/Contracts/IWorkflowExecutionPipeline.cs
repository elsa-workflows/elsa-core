using Elsa.Workflows.Pipelines.WorkflowExecution;

namespace Elsa.Workflows;

/// <summary>
/// Represents a workflow execution pipeline.
/// </summary>
public interface IWorkflowExecutionPipeline
{
    /// <summary>
    /// The pipeline builder factory.
    /// </summary>
    Action<IWorkflowExecutionPipelineBuilder> ConfigurePipelineBuilder { get; }
    
    /// <summary>
    /// Sets up the pipeline using the specified pipeline builder factory.
    /// </summary>
    WorkflowMiddlewareDelegate Setup(Action<IWorkflowExecutionPipelineBuilder> setup);
    
    /// <summary>
    /// The constructed pipeline delegate.
    /// </summary>
    WorkflowMiddlewareDelegate Pipeline { get; }
    
    /// <summary>
    /// Executes the pipeline with the specified workflow execution context.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task ExecuteAsync(WorkflowExecutionContext context);
}