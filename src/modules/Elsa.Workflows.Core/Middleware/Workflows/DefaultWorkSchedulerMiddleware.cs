using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;

namespace Elsa.Workflows.Core.Middleware.Workflows;

/// <summary>
/// Installs middleware that executes scheduled work items.
/// </summary>
public static class UseActivitySchedulerMiddlewareExtensions
{
    /// <summary>
    /// Installs middleware that executes scheduled work items. 
    /// </summary>
    public static IWorkflowExecutionPipelineBuilder UseDefaultActivityScheduler(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<DefaultWorkSchedulerMiddleware>();
}

/// <summary>
/// A workflow execution middleware component that executes scheduled work items.
/// </summary>
public class DefaultWorkSchedulerMiddleware : WorkflowExecutionMiddleware
{
    /// <inheritdoc />
    public DefaultWorkSchedulerMiddleware(WorkflowMiddlewareDelegate next) : base(next)
    {
    }

    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        var scheduler = context.Scheduler;

        // Transition into the Executing state.
        context.TransitionTo(WorkflowSubStatus.Executing);

        // As long as there are work items scheduled, keep executing them.
        while (scheduler.HasAny)
        {
            // Pop next work item for execution.
            var currentWorkItem = scheduler.Take();

            // Execute work item.
            await currentWorkItem.Execute();
        }

        // Invoke next middleware.
        await Next(context);

        // If all activities are completed, complete the workflow.
        if (context.Status == WorkflowStatus.Running)
            context.TransitionTo(context.AllActivitiesCompleted() ? WorkflowSubStatus.Finished : WorkflowSubStatus.Suspended);
    }
}