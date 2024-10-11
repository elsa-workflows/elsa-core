using Elsa.Extensions;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Pipelines.WorkflowExecution;

namespace Elsa.Workflows.Middleware.Workflows;

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
            // Do not start a workflow if cancellation has been requested.
            if (context.CancellationToken.IsCancellationRequested)
                break;
            
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
            Variables = workItem.Variables,
            Input = workItem.Input
        };

        await _activityInvoker.InvokeAsync(context, workItem.Activity, options);
    }
}