using Elsa.Workflows.Contracts;
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
            .UseEngineExceptionHandling()
            .UseBackgroundActivities()
            .UseBookmarkPersistence()
            .UseActivityExecutionLogPersistence()
            .UseWorkflowExecutionLogPersistence()
            .UsePersistentVariables()
            .UseExceptionHandling()
            .UseDefaultActivityScheduler();

    /// <summary>
    /// Installs middleware that persists the workflow instance before and after workflow execution.
    /// </summary>
    public static IWorkflowExecutionPipelineBuilder UseBackgroundActivities(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<ScheduleBackgroundActivitiesMiddleware>();
    
    /// <summary>
    /// Installs middleware that persists the workflow instance before and after workflow execution.
    /// </summary>
    public static IWorkflowExecutionPipelineBuilder UsePersistentVariables(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<PersistentVariablesMiddleware>();
    
    /// <summary>
    /// Installs middleware that persists bookmarks after workflow execution.
    /// </summary>
    public static IWorkflowExecutionPipelineBuilder UseBookmarkPersistence(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<PersistBookmarkMiddleware>();

    /// <summary>
    /// Installs middleware that persist the workflow execution journal.
    /// </summary>
    public static IWorkflowExecutionPipelineBuilder UseWorkflowExecutionLogPersistence(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<PersistWorkflowExecutionLogMiddleware>();
    
    /// <summary>
    /// Installs middleware that persist activity execution records.
    /// </summary>
    public static IWorkflowExecutionPipelineBuilder UseActivityExecutionLogPersistence(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<PersistActivityExecutionLogMiddleware>();
}