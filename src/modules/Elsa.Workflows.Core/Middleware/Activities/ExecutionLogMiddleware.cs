using Elsa.Workflows.Models;
using Elsa.Workflows.Pipelines.ActivityExecution;
using JetBrains.Annotations;

namespace Elsa.Workflows.Middleware.Activities;

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
/// An activity execution middleware component that extracts execution details as <see cref="WorkflowExecutionLogEntry"/> objects.
/// </summary>
[UsedImplicitly]
public class ExecutionLogMiddleware(ActivityMiddlewareDelegate next) : IActivityExecutionMiddleware
{
    /// <inheritdoc />
    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        context.AddExecutionLogEntry(IsActivityBookmarked(context) ? "Resumed" : "Started");

        try
        {
            await next(context);

            if (context.Status == ActivityStatus.Running)
            {
                if (IsActivityBookmarked(context))
                    context.AddExecutionLogEntry("Suspended", payload: context.JournalData);
            }
        }
        catch (Exception exception)
        {
            context.AddExecutionLogEntry("Faulted",
                message: exception.Message,
                payload: new
                {
                    Exception = exception.GetType().FullName,
                    exception.Message,
                    exception.Source,
                    exception.Data,
                    exception.StackTrace,
                    InnerException = exception.InnerException?.GetType().FullName,
                });

            throw;
        }
    }

    private static bool IsActivityBookmarked(ActivityExecutionContext context)
    {
        return context.WorkflowExecutionContext.Bookmarks.Any(b => b.ActivityNodeId.Equals(context.ActivityNode.NodeId));
    }
}