using Elsa.Workflows.Contracts;
using Elsa.Workflows.Middleware.Workflows;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime.Middleware.Workflows;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// Provides extensions to <see cref="IWorkflowExecutionPipelineBuilder"/> that add various middleware components.
public static class WorkflowExecutionPipelineBuilderExtensions
{
    /// Configures the workflow execution pipeline with commonly used components.
    public static IWorkflowExecutionPipelineBuilder UseDefaultPipeline(this IWorkflowExecutionPipelineBuilder pipelineBuilder) =>
        pipelineBuilder
            .Reset()
            .UseDeferredActivityTasks()
            .UseBackgroundActivities()
            .UseBookmarkPersistence()
            .UseActivityExecutionLogPersistence()
            .UseWorkflowExecutionLogPersistence()
            .UsePersistentVariables()
            .UseExceptionHandling()
            .UseDefaultActivityScheduler();

    /// Installs middleware that persists the workflow instance before and after workflow execution.
    public static IWorkflowExecutionPipelineBuilder UseBackgroundActivities(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<ScheduleBackgroundActivitiesMiddleware>();

    /// Installs middleware that persists the workflow instance before and after workflow execution.
    public static IWorkflowExecutionPipelineBuilder UsePersistentVariables(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<PersistentVariablesMiddleware>();

    /// Installs middleware that persists bookmarks after workflow execution.
    public static IWorkflowExecutionPipelineBuilder UseBookmarkPersistence(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<PersistBookmarkMiddleware>();

    /// Installs middleware that executes deferred activity tasks.
    public static IWorkflowExecutionPipelineBuilder UseDeferredActivityTasks(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<ExecuteDeferredActivityTasks>();

    /// Installs middleware that persists the workflow execution journal.
    public static IWorkflowExecutionPipelineBuilder UseWorkflowExecutionLogPersistence(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<PersistWorkflowExecutionLogMiddleware>();

    /// Installs middleware that persists activity execution records.
    public static IWorkflowExecutionPipelineBuilder UseActivityExecutionLogPersistence(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<PersistActivityExecutionLogMiddleware>();
}