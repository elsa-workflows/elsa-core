using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.ActivityExecution;

namespace Elsa.Workflows.Core.Middleware.Activities;

/// <summary>
/// Adds extension methods to <see cref="ExecutionLogMiddleware"/>.
/// </summary>
public static class ExecutionLogMiddlewareExtensions
{
    /// <summary>
    /// Installs the <see cref="ExecutionLogMiddleware"/> component in the activity execution pipeline.
    /// </summary>
    public static IActivityExecutionPipelineBuilder UseExecutionLogging(this IActivityExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<ExecutionLogMiddleware>();
}

/// <summary>
/// An activity execution middleware component that extracts execution details as <see cref="WorkflowExecutionLogEntry"/>.
/// </summary>
public class ExecutionLogMiddleware : IActivityExecutionMiddleware
{
    private readonly ActivityMiddlewareDelegate _next;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ExecutionLogMiddleware(ActivityMiddlewareDelegate next)
    {
        _next = next;
    }

    /// <inheritdoc />
    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        context.AddExecutionLogEntry(IsActivityBookmarked(context) ? "Resumed" : "Started", includeActivityState: true);

        try
        {
            await _next(context);

            if (context.Status == ActivityStatus.Running)
            {
                if (IsActivityBookmarked(context))
                    context.AddExecutionLogEntry("Suspended", payload: context.JournalData, includeActivityState: true);
            }
        }
        catch (Exception exception)
        {
            context.Status = ActivityStatus.Faulted;
            context.AddExecutionLogEntry("Faulted",
                includeActivityState: true,
                payload: new
                {
                    Exception = new
                    {
                        exception.Message,
                        exception.Source,
                        exception.Data,
                        Type = exception.GetType()
                    }
                });

            throw;
        }
    }

    private static bool IsActivityBookmarked(ActivityExecutionContext context) =>
        context.WorkflowExecutionContext.Bookmarks.Any(b => b.ActivityNodeId.Equals(context.ActivityNode.NodeId));
}