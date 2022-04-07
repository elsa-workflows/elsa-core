using Elsa.Contracts;
using Elsa.Pipelines.WorkflowExecution;
using Elsa.Runtime.Middleware;
using Elsa.Runtime.Notifications;

namespace Elsa.Runtime.Extensions;

public static class WorkflowExecutionPipelineBuilderExtensions
{
    /// <summary>
    /// Installs middleware that persists the workflow instance before and after workflow execution.
    /// </summary>
    public static IWorkflowExecutionBuilder UsePersistence(this IWorkflowExecutionBuilder builder) => builder.UseMiddleware<PersistWorkflowInstanceMiddleware>();
    
    /// <summary>
    /// Installs middleware that publishes the <see cref="WorkflowExecuting"/> and <see cref="WorkflowExecuted"/> events.
    /// </summary>
    public static IWorkflowExecutionBuilder UseWorkflowExecutionEvents(this IWorkflowExecutionBuilder builder) => builder.UseMiddleware<PublishWorkflowExecutionEventsMiddleware>();
    
    /// <summary>
    /// Installs middleware that persist the workflow execution journal.
    /// </summary>
    public static IWorkflowExecutionBuilder UseWorkflowExecutionLogPersistence(this IWorkflowExecutionBuilder builder) => builder.UseMiddleware<PersistWorkflowExecutionLogMiddleware>();
}