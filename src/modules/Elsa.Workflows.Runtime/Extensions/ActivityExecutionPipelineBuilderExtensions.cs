using Elsa.Workflows.Contracts;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.Runtime.Middleware.Activities;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extensions to <see cref="IActivityExecutionPipelineBuilder"/>.
/// </summary>
public static class ActivityExecutionPipelineBuilderExtensions
{
    /// <summary>
    /// Installs the <see cref="BackgroundActivityInvokerMiddleware"/>.
    /// </summary>
    public static IActivityExecutionPipelineBuilder UseBackgroundActivityInvoker(this IActivityExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<BackgroundActivityInvokerMiddleware>();
}