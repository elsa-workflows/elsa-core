using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Middleware.Workflows;

public static class UseActivitySchedulerMiddlewareExtensions
{
    /// <summary>
    /// Installs middleware that executes scheduled work items (activities). 
    /// </summary>
    public static IWorkflowExecutionBuilder UseDefaultActivityScheduler(this IWorkflowExecutionBuilder builder) => builder.UseMiddleware<DefaultActivitySchedulerMiddleware>();
}

public class DefaultActivitySchedulerMiddleware : WorkflowExecutionMiddleware
{
    public DefaultActivitySchedulerMiddleware(WorkflowMiddlewareDelegate next) : base(next)
    {
    }

    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        var scheduler = context.Scheduler;
        
        // Transition into the Executing state.
        context.TransitionTo(WorkflowSubStatus.Executing);

        // As long as there are activities scheduled, keep executing them.
        while (scheduler.HasAny)
        {
            // Pop next work item for execution.
            var currentWorkItem = scheduler.Take();

            // Execute work item.
            await currentWorkItem.Execute();
        }

        // Invoke next middleware.
        await Next(context);
        
        // If there are no bookmarks and all activities are completed, complete the workflow.
        if (context.Status == WorkflowStatus.Running)
        {
            if (!context.Bookmarks.Any())
            {
                context.TransitionTo(WorkflowSubStatus.Finished);
            }
            else
            {
                context.TransitionTo(WorkflowSubStatus.Suspended);
            }
        }
    }
}