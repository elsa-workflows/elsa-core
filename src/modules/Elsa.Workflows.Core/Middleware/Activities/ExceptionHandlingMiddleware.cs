using Elsa.Common;
using Elsa.Extensions;
using Elsa.Workflows.Models;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.State;
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
public class ExceptionHandlingMiddleware(ActivityMiddlewareDelegate next, IIncidentStrategyResolver incidentStrategyResolver, ISystemClock systemClock, ILogger<ExceptionHandlingMiddleware> logger)
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
            LogExceptionAndTransition(context, e);
            FaultAncestors(context);
            await HandleIncidentAsync(context);
        }
    }

    private void LogExceptionAndTransition(ActivityExecutionContext context, Exception e)
    {
        context.Exception = e;
        context.TransitionTo(ActivityStatus.Faulted);
        var activity = context.Activity;
        var exceptionState = ExceptionState.FromException(e);
        var now = systemClock.UtcNow;
        var incident = new ActivityIncident(activity.Id, activity.Type, e.Message, exceptionState, now);
        context.WorkflowExecutionContext.Incidents.Add(incident);
    }

    private async Task HandleIncidentAsync(ActivityExecutionContext context)
    {
        var strategy = await incidentStrategyResolver.ResolveStrategyAsync(context);
        strategy.HandleIncident(context);
    }

    private static void FaultAncestors(ActivityExecutionContext context)
    {
        var ancestors = context.GetAncestors();

        foreach (var ancestor in ancestors)
            ancestor.TransitionTo(ActivityStatus.Faulted);
    }
}