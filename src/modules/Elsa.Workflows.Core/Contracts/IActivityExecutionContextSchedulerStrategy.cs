using Elsa.Workflows.Models;
using Elsa.Workflows.Options;

namespace Elsa.Workflows;

public interface IActivityExecutionContextSchedulerStrategy
{
    Task ScheduleActivityAsync(
        ActivityExecutionContext context,
        IActivity? activity, 
        ActivityExecutionContext? owner, 
        ScheduleWorkOptions? options = null);
    
    Task ScheduleActivityAsync(
        ActivityExecutionContext context, 
        ActivityNode? activityNode, 
        ActivityExecutionContext? owner = null, 
        ScheduleWorkOptions? options = null);
}