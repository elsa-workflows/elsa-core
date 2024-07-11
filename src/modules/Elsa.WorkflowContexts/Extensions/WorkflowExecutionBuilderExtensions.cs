using Elsa.WorkflowContexts.Middleware;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.Pipelines.WorkflowExecution;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extension methods for <see cref="IWorkflowExecutionPipelineBuilder"/>.
/// </summary>
public static class WorkflowExecutionBuilderExtensions
{
    /// <summary>
    /// Installs middleware that enables the use of workflow context.
    /// </summary>
    public static IWorkflowExecutionPipelineBuilder UseWorkflowContexts(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<WorkflowContextWorkflowExecutionMiddleware>();
    
    /// <summary>
    /// Installs middleware that enables the use of workflow context.
    /// </summary>
    public static IActivityExecutionPipelineBuilder UseWorkflowContexts(this IActivityExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<WorkflowContextActivityExecutionMiddleware>();
}