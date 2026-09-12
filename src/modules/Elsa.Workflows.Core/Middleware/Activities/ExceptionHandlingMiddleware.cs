using Elsa.Extensions;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.Signals;
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
/// Catches any exceptions thrown by downstream components and transitions the workflow into the faulted state,
/// unless an enclosing container claims the fault by handling the <see cref="FaultSignal"/>.
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

            // Give the ancestor chain a chance to handle the fault. See FaultSignal for the contract: the handler owns
            // terminalizing the faulted activity, and recovering the fault bookkeeping is ours alone to do, exactly once.
            if (await TryHandOffToAncestorsAsync(context, e))
            {
                context.RecoverFromFault();
                return;
            }

            await HandleIncidentAsync(context);
        }
    }

    /// <summary>
    /// Offers the fault to the ancestor chain, and reports whether one of them claimed it.
    /// </summary>
    /// <remarks>
    /// A handler that throws is treated as not having handled the fault, so the incident strategy runs exactly as it
    /// would if no handler existed. This is the one place where swallowing an exception is right: the send happens
    /// inside the <c>catch</c> whose entire job is to stop exceptions escaping the activity pipeline, so letting a
    /// handler's failure through would defeat the middleware and lose the original fault along with it. Falling through
    /// to the incident strategy is also the conservative direction: a handler that failed part way through may have left
    /// the faulted activity in any state, and surfacing that as an incident is better than reporting success. Both
    /// exceptions are logged, the handler's at error level, because a broken fault handler is a defect in its own right
    /// rather than a workflow outcome.
    /// <para>
    /// Cancellation is deliberately excluded. An <see cref="OperationCanceledException"/> from a handler means the host
    /// is tearing the run down, not that the handler is broken, and this repository consistently keeps the two apart:
    /// the workflow-level exception middleware cancels and rethrows before its general catch, and
    /// <c>WorkflowRunner</c> declines to record cancellation as the workflow's exception. Swallowing it here would
    /// convert a deliberate cancellation into a faulted workflow.
    /// </para>
    /// </remarks>
    private async Task<bool> TryHandOffToAncestorsAsync(ActivityExecutionContext context, Exception fault)
    {
        try
        {
            return await context.TrySendSignalAsync(new FaultSignal(fault, context));
        }
        catch (Exception handlerException) when (handlerException is not OperationCanceledException)
        {
            logger.LogError(
                handlerException,
                "A {SignalType} handler threw while an ancestor of activity {ActivityId} of type {ActivityType} was being offered its fault. Treating the fault as unhandled",
                nameof(FaultSignal),
                context.Activity.Id,
                context.Activity.Type);

            return false;
        }
    }

    private async Task HandleIncidentAsync(ActivityExecutionContext context)
    {
        var strategy = await incidentStrategyResolver.ResolveStrategyAsync(context);
        strategy.HandleIncident(context);
    }
}