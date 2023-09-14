using Elsa.Common.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.ActivityExecution;
using Elsa.Workflows.Core.State;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Core.Middleware.Activities;

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
public class ExceptionHandlingMiddleware : IActivityExecutionMiddleware
{
    private readonly ActivityMiddlewareDelegate _next;
    private readonly IIncidentStrategyResolver _incidentStrategyResolver;
    private readonly ISystemClock _systemClock;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ExceptionHandlingMiddleware(ActivityMiddlewareDelegate next, IIncidentStrategyResolver incidentStrategyResolver, ISystemClock systemClock, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _incidentStrategyResolver = incidentStrategyResolver;
        _systemClock = systemClock;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "An exception was caught from a downstream middleware component");
            context.Exception = e;
            context.Status = ActivityStatus.Faulted;

            var activity = context.Activity;
            var exceptionState = ExceptionState.FromException(e);
            var now = _systemClock.UtcNow;
            var incident = new ActivityIncident(activity.Id, activity.Type, e.Message, exceptionState, now);
            context.WorkflowExecutionContext.Incidents.Add(incident);

            var strategy = await _incidentStrategyResolver.ResolveStrategyAsync(context);
            strategy.HandleIncident(context);
        }
    }
}