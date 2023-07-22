using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Middleware.Workflows;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
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
    public static IWorkflowExecutionPipelineBuilder UseDefaultRuntimePipeline(this IWorkflowExecutionPipelineBuilder pipelineBuilder) =>
        pipelineBuilder
            .Reset()
            .UsePersistentVariables()
            .UseBookmarkPersistence()
            .UseActivityExecutionLogPersistence()
            .UseWorkflowExecutionLogPersistence()
            .UseWorkflowStatePersistence()
            .UseDefaultActivityScheduler();

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
    
    /// <summary>
    /// Installs middleware that persist the workflow execution state.
    /// </summary>
    public static IWorkflowExecutionPipelineBuilder UseWorkflowStatePersistence(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<PersistWorkflowStateMiddleware>();
}