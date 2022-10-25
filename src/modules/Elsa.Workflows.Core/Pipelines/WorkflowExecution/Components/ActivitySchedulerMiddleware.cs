using System.Linq;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Pipelines.WorkflowExecution.Components;

public static class UseActivitySchedulerMiddlewareExtensions
{
    /// <summary>
    /// Installs middleware that executes scheduled work items (activities). 
    /// </summary>
    public static IWorkflowExecutionBuilder UseStackBasedActivityScheduler(this IWorkflowExecutionBuilder builder) => builder.UseMiddleware<ActivitySchedulerMiddleware>();
}

public class ActivitySchedulerMiddleware : WorkflowExecutionMiddleware
{
    public ActivitySchedulerMiddleware(WorkflowMiddlewareDelegate next) : base(next)
    {
    }

    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        var scheduler = context.Scheduler;

        // As long as there are activities scheduled, keep executing them.
        while (scheduler.HasAny)
        {
            // Pop next work item for execution.
            var currentWorkItem = scheduler.Take();

            // Execute work item.
            await currentWorkItem.Execute();
        }

        // If there are no bookmarks and all activities are completed, complete the workflow.
        if (context.Status == WorkflowStatus.Running)
        {
            if (!context.Bookmarks.Any())
            {
                context.TransitionTo(WorkflowSubStatus.Finished);
            }
        }

        // Invoke next middleware.
        await Next(context);
    }
}