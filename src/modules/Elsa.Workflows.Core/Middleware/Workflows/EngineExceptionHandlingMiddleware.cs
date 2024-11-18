using Elsa.Common;
using Elsa.Workflows.Models;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.State;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Middleware.Workflows;

/// <summary>
/// Adds extension methods to <see cref="ExceptionHandlingMiddleware"/>.
/// </summary>
public static class EngineExceptionHandlingMiddlewareExtensions
{
    /// <summary>
    /// Installs the <see cref="ExceptionHandlingMiddleware"/> component in the activity execution pipeline.
    /// </summary>
    public static IWorkflowExecutionPipelineBuilder UseEngineExceptionHandling(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<EngineExceptionHandlingMiddleware>();
}

/// <summary>
/// Catches any exceptions thrown by downstream components and transitions the workflow into the faulted state.
/// </summary>
public class EngineExceptionHandlingMiddleware(WorkflowMiddlewareDelegate next, ISystemClock systemClock, ILogger<EngineExceptionHandlingMiddleware> logger) : IWorkflowExecutionMiddleware
{
    /// <inheritdoc />
    public async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "An exception was caught from a downstream middleware component");
            var exceptionState = ExceptionState.FromException(e);
            var now = systemClock.UtcNow;
            var activity = context.Workflow;
            var incident = new ActivityIncident(activity.Id, activity.Type, e.Message, exceptionState, now);
            
            // No state change as the workflow / activities status should be leading.
            // We will however be adding an incident to make the issue visible.
            context.Incidents.Add(incident);
            context.AddExecutionLogEntry("Faulted", e.Message, exceptionState);
        }
    }
}