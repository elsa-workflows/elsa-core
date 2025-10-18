using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;

namespace Elsa.Testing.Shared;

public class FakeActivityExecutionContextSchedulerStrategy : IActivityExecutionContextSchedulerStrategy
{
    public Task ScheduleActivityAsync(ActivityExecutionContext context, IActivity? activity, ActivityExecutionContext? owner, ScheduleWorkOptions? options = null)
    {
        if (activity == null)
            return Task.CompletedTask;
        
        var activityNode = new ActivityNode(activity!, "");
        return ScheduleActivityAsync(context, activityNode, owner, options);
    }

    public Task ScheduleActivityAsync(ActivityExecutionContext context, ActivityNode? activityNode, ActivityExecutionContext? owner = null, ScheduleWorkOptions? options = null)
    {
        if (activityNode == null)
            return Task.CompletedTask;
        
        context.WorkflowExecutionContext.Schedule(activityNode!, owner ?? context, options);
        return Task.CompletedTask;
    }
}