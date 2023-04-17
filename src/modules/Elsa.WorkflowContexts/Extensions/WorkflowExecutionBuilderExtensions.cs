using Elsa.WorkflowContexts.Middleware;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Pipelines.ActivityExecution;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;

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