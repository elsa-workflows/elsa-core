using Elsa.Tenants.Middlewares;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Pipelines.ActivityExecution;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;

namespace Elsa.Tenants.Extensions;

/// <summary>
/// TODO JBD : Does not work for now, need to be put at a higher level in the workflow pipeline to let DefaultWorkflowRuntime.CreateWorkflowHostAsync
/// or BackgroundWorkflowDispatcher or DispatchWorkflowRequestHandler find the definitions in the tenants.
/// Extension methods for <see cref="IWorkflowExecutionPipelineBuilder"/>.
/// </summary>
public static class WorkflowExecutionBuilderExtensions
{
    /// <summary>
    /// Installs middleware that enables the use of workflow context.
    /// </summary>
    public static IWorkflowExecutionPipelineBuilder UseWorkflowTenantContext(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<WorkflowContextTenantExecutionMiddleware>();
}