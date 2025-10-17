using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Options;

namespace Elsa.Activities.UnitTests.Helpers;

public class FakeActivityExecutionContextSchedulerStrategy : IActivityExecutionContextSchedulerStrategy
{
    public Task ScheduleActivityAsync(ActivityExecutionContext context, IActivity? activity, ActivityExecutionContext? owner, ScheduleWorkOptions? options = null)
    {
        var activityNode = new ActivityNode(activity!, "");
        return ScheduleActivityAsync(context, activityNode, owner, options);
    }

    public Task ScheduleActivityAsync(ActivityExecutionContext context, ActivityNode? activityNode, ActivityExecutionContext? owner = null, ScheduleWorkOptions? options = null)
    {
        context.WorkflowExecutionContext.Schedule(activityNode!, owner ?? context, options);
        return Task.CompletedTask;
    }
}