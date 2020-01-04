using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Primitives;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Extensions
{
    public static class SchedulerExtensions
    {
        public static Task<WorkflowExecutionContext> ScheduleActivityAsync(this IScheduler scheduler, Action activity, object? input = default, CancellationToken cancellationToken = default) 
            => scheduler.ScheduleActivityAsync(new Inline(activity), Variable.From(input), cancellationToken);
        
        public static Task<WorkflowExecutionContext> ScheduleActivityAsync(this IScheduler scheduler, Action<WorkflowExecutionContext, ActivityExecutionContext> activity, object? input = default, CancellationToken cancellationToken = default) 
            => scheduler.ScheduleActivityAsync(new Inline(activity), Variable.From(input), cancellationToken);
        
        public static Task<WorkflowExecutionContext> ScheduleActivityAsync(this IScheduler scheduler, Func<WorkflowExecutionContext, ActivityExecutionContext, Task> activity, object? input = default, CancellationToken cancellationToken = default) 
            => scheduler.ScheduleActivityAsync(new Inline(activity), Variable.From(input), cancellationToken);
    }
}