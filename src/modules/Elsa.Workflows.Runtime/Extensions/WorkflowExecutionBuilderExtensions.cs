using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Middleware;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class WorkflowExecutionPipelineBuilderExtensions
{
    /// <summary>
    /// Installs middleware that persists the workflow instance before and after workflow execution.
    /// </summary>
    public static IWorkflowExecutionBuilder UsePersistentVariables(this IWorkflowExecutionBuilder builder) => builder.UseMiddleware<PersistentVariablesMiddleware>();
    
    /// <summary>
    /// Installs middleware that persists bookmarks after workflow execution.
    /// </summary>
    public static IWorkflowExecutionBuilder UseBookmarkPersistence(this IWorkflowExecutionBuilder builder) => builder.UseMiddleware<PersistBookmarkMiddleware>();

    /// <summary>
    /// Installs middleware that persist the workflow execution journal.
    /// </summary>
    public static IWorkflowExecutionBuilder UseWorkflowExecutionLogPersistence(this IWorkflowExecutionBuilder builder) => builder.UseMiddleware<PersistWorkflowExecutionLogMiddleware>();
}