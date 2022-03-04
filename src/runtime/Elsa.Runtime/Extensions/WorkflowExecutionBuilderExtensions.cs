using Elsa.Contracts;
using Elsa.Pipelines.WorkflowExecution;
using Elsa.Runtime.Middleware;

namespace Elsa.Runtime.Extensions;

public static class WorkflowExecutionPipelineBuilderExtensions
{
    public static IWorkflowExecutionBuilder UsePersistence(this IWorkflowExecutionBuilder builder) => builder.UseMiddleware<PersistWorkflowInstanceMiddleware>();
    public static IWorkflowExecutionBuilder UseWorkflowExecutionEvents(this IWorkflowExecutionBuilder builder) => builder.UseMiddleware<PublishWorkflowExecutionEventsMiddleware>();
    public static IWorkflowExecutionBuilder UseWorkflowExecutionLogPersistence(this IWorkflowExecutionBuilder builder) => builder.UseMiddleware<PersistWorkflowExecutionLogMiddleware>();
}