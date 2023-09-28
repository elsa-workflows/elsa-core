using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Options;
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
    private readonly IActivityInvoker _activityInvoker;

    /// <inheritdoc />
    public DefaultWorkSchedulerMiddleware(WorkflowMiddlewareDelegate next, IActivityInvoker activityInvoker) : base(next)
    {
        _activityInvoker = activityInvoker;
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
            //await currentWorkItem.Execute();
            await ExecuteWorkItemAsync(context, currentWorkItem);
        }

        // Invoke next middleware.
        await Next(context);

        // If all activities are completed, complete the workflow.
        if (context.Status == WorkflowStatus.Running)
            context.TransitionTo(context.AllActivitiesCompleted() ? WorkflowSubStatus.Finished : WorkflowSubStatus.Suspended);
    }

    private async Task ExecuteWorkItemAsync(WorkflowExecutionContext context, ActivityWorkItem workItem)
    {
        var options = new ActivityInvocationOptions
        {
            Owner = workItem.Owner,
            ReuseActivityExecutionContextId = workItem.ReuseActivityExecutionContextId,
            Tag = workItem.Tag,
            Variables = workItem.Variables
        };

        await _activityInvoker.InvokeAsync(context, workItem.Activity, options);
    }
}