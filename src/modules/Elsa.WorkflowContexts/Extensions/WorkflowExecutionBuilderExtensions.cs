using Elsa.Pipelines.ActivityExecution;
using Elsa.Pipelines.WorkflowExecution;
using Elsa.Services;
using Elsa.WorkflowContexts.Middleware;

namespace Elsa.WorkflowContexts.Extensions;

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