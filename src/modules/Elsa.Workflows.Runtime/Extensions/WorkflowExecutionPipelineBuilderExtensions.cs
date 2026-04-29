using Elsa.Workflows;
using Elsa.Workflows.Middleware.Workflows;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime.Middleware.Workflows;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions to <see cref="IWorkflowExecutionPipelineBuilder"/> that add various middleware components.
/// </summary>
public static class WorkflowExecutionPipelineBuilderExtensions
{
    /// <summary>
    /// Configures the workflow execution pipeline with commonly used components.
    /// </summary>
    public static IWorkflowExecutionPipelineBuilder UseDefaultPipeline(this IWorkflowExecutionPipelineBuilder pipelineBuilder) =>
        pipelineBuilder
            .Reset()
            .UseExecutionCycleTracking()
            .UseWorkflowHeartbeat()
            .UseEngineExceptionHandling()
            .UsePersistentVariables()
            .UseExceptionHandling()
            .UseDefaultActivityScheduler();

    /// <summary>
    /// Installs middleware that persists the workflow instance before and after workflow execution.
    /// </summary>
    public static IWorkflowExecutionPipelineBuilder UsePersistentVariables(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<PersistentVariablesMiddleware>();

    public static IWorkflowExecutionPipelineBuilder UseWorkflowHeartbeat(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<WorkflowHeartbeatMiddleware>();

    /// <summary>
    /// Installs middleware that registers an <see cref="Elsa.Workflows.Runtime.ExecutionCycleHandle"/> for the duration of
    /// each workflow execution cycle, so the drain orchestrator can count active executions and force-cancel them on
    /// deadline breach.
    /// </summary>
    public static IWorkflowExecutionPipelineBuilder UseExecutionCycleTracking(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<ExecutionCycleTrackingMiddleware>();

    /// <summary>
    /// Installs middleware that persists bookmarks after workflow execution.
    /// </summary>
    [Obsolete("This middleware is no longer used and will be removed in a future version. Bookmarks are now persisted through the commit state handler.")]
    public static IWorkflowExecutionPipelineBuilder UseBookmarkPersistence(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<PersistBookmarkMiddleware>();

    /// <summary>
    /// Installs middleware that persists the workflow execution journal.
    /// </summary>
    [Obsolete("This middleware is no longer used and will be removed in a future version. Execution logs are now persisted through the commit state handler.")]
    public static IWorkflowExecutionPipelineBuilder UseWorkflowExecutionLogPersistence(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<PersistWorkflowExecutionLogMiddleware>();

    /// <summary>
    /// Installs middleware that persists activity execution records.
    /// </summary>
    [Obsolete("This middleware is no longer used and will be removed in a future version. Activity state is now persisted through the commit state handler.")]
    public static IWorkflowExecutionPipelineBuilder UseActivityExecutionLogPersistence(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<PersistActivityExecutionLogMiddleware>();
}