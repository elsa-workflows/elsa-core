using Elsa.Extensions;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Middleware.Activities;

/// <summary>
/// Adds extension methods to <see cref="ExceptionHandlingMiddleware"/>.
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    /// <summary>
    /// Installs the <see cref="ExceptionHandlingMiddleware"/> component in the activity execution pipeline.
    /// </summary>
    public static IActivityExecutionPipelineBuilder UseExceptionHandling(this IActivityExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<ExceptionHandlingMiddleware>();
}

/// <summary>
/// Catches any exceptions thrown by downstream components and transitions the workflow into the faulted state.
/// </summary>
public class ExceptionHandlingMiddleware(ActivityMiddlewareDelegate next, IIncidentStrategyResolver incidentStrategyResolver, ILogger<ExceptionHandlingMiddleware> logger)
    : IActivityExecutionMiddleware
{
    /// <inheritdoc />
    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "An exception was caught from a downstream middleware component");
            context.Fault(e);
            await HandleIncidentAsync(context);
        }
    }

    private async Task HandleIncidentAsync(ActivityExecutionContext context)
    {
        var strategy = await incidentStrategyResolver.ResolveStrategyAsync(context);
        strategy.HandleIncident(context);
    }
}