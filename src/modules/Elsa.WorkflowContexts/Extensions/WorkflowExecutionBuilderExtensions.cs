using Elsa.WorkflowContexts.Middleware;
using Elsa.Workflows.Core.Pipelines.ActivityExecution;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
using Elsa.Workflows.Core.Services;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class WorkflowExecutionBuilderExtensions
{
    /// <summary>
    /// Installs middleware that enables the use of workflow context.
    /// </summary>
    public static IWorkflowExecutionBuilder UseWorkflowContexts(this IWorkflowExecutionBuilder builder) => builder.UseMiddleware<WorkflowContextWorkflowExecutionMiddleware>();
    
    /// <summary>
    /// Installs middleware that enables the use of workflow context.
    /// </summary>
    public static IActivityExecutionBuilder UseWorkflowContexts(this IActivityExecutionBuilder builder) => builder.UseMiddleware<WorkflowContextActivityExecutionMiddleware>();
}