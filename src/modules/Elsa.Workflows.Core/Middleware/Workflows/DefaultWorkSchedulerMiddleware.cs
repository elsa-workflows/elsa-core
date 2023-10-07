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
    public static IWorkflowExecutionPipelineBuilder UseDefaultActivityScheduler(this IWorkflowExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<DefaultActivitySchedulerMiddleware>();
}

/// <summary>
/// A workflow execution middleware component that executes scheduled work items.
/// </summary>
public class DefaultActivitySchedulerMiddleware : WorkflowExecutionMiddleware
{
    private readonly IActivityInvoker _activityInvoker;

    /// <inheritdoc />
    public DefaultActivitySchedulerMiddleware(WorkflowMiddlewareDelegate next, IActivityInvoker activityInvoker) : base(next)
    {
        _activityInvoker = activityInvoker;
    }

    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        var scheduler = context.Scheduler;

        context.TransitionTo(WorkflowSubStatus.Executing);
        
        while (scheduler.HasAny)
        {
            var currentWorkItem = scheduler.Take();
            await ExecuteWorkItemAsync(context, currentWorkItem);
        }
        
        await Next(context);
        
        if (context.Status == WorkflowStatus.Running)
            context.TransitionTo(context.AllActivitiesCompleted() ? WorkflowSubStatus.Finished : WorkflowSubStatus.Suspended);
    }

    private async Task ExecuteWorkItemAsync(WorkflowExecutionContext context, ActivityWorkItem workItem)
    {
        var options = new ActivityInvocationOptions
        {
            Owner = workItem.Owner,
            ExistingActivityExecutionContext = workItem.ExistingActivityExecutionContext,
            Tag = workItem.Tag,
            Variables = workItem.Variables
        };

        await _activityInvoker.InvokeAsync(context, workItem.Activity, options);
    }
}