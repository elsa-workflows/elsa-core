using Elsa.Contracts;
using Elsa.Modules.WorkflowContexts.Middleware;
using Elsa.Pipelines.WorkflowExecution;

namespace Elsa.Modules.WorkflowContexts.Extensions;

public static class WorkflowExecutionBuilderExtensions
{
    /// <summary>
    /// Installs the <see cref="WorkflowContextMiddleware"/>.
    /// </summary>
    public static IWorkflowExecutionBuilder UseWorkflowContexts(this IWorkflowExecutionBuilder builder) => builder.UseMiddleware<WorkflowContextMiddleware>();
}