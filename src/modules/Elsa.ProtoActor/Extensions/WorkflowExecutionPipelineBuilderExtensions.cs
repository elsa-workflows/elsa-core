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
    /// Configures the workflow execution pipeline with commonly used components for Proto Actor.
    /// </summary>
    public static IWorkflowExecutionPipelineBuilder UseProtoActorRuntimePipeline(this IWorkflowExecutionPipelineBuilder pipelineBuilder) =>
        pipelineBuilder
            .Reset()
            .UsePersistentVariables()
            .UseBookmarkPersistence()
            .UseWorkflowExecutionLogPersistence()
            .UseDefaultActivityScheduler();
}