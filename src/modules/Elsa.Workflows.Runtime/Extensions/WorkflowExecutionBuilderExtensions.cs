using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Middleware;

namespace Elsa.Workflows.Runtime.Extensions;

public static class WorkflowExecutionPipelineBuilderExtensions
{
    /// <summary>
    /// Installs middleware that persists the workflow instance before and after workflow execution.
    /// </summary>
    public static IWorkflowExecutionBuilder UsePersistentVariables(this IWorkflowExecutionBuilder builder) => builder.UseMiddleware<PersistentVariablesMiddleware>();

    /// <summary>
    /// Installs middleware that persist the workflow execution journal.
    /// </summary>
    public static IWorkflowExecutionBuilder UseWorkflowExecutionLogPersistence(this IWorkflowExecutionBuilder builder) => builder.UseMiddleware<PersistWorkflowExecutionLogMiddleware>();
}