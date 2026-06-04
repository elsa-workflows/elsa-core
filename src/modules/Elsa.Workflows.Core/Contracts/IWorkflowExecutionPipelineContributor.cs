using Elsa.Workflows.Pipelines.WorkflowExecution;

namespace Elsa.Workflows;

/// <summary>
/// Contributes middleware to the workflow execution pipeline.
/// </summary>
public interface IWorkflowExecutionPipelineContributor
{
    /// <summary>
    /// Gets the order in which the contributor is applied.
    /// </summary>
    int Order => 0;

    /// <summary>
    /// Configures the workflow execution pipeline.
    /// </summary>
    void Configure(IWorkflowExecutionPipelineBuilder builder);
}
