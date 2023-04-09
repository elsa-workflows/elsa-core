using Elsa.Extensions;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Middleware.Activities;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

/// <summary>
/// Schedules background activities for execution. This component works in tandem with <see cref="BackgroundActivityInvokerMiddleware"/>.
/// </summary>
public class ScheduleBackgroundActivitiesMiddleware : WorkflowExecutionMiddleware
{
    private readonly IBackgroundActivityScheduler _backgroundActivityScheduler;
    internal static readonly object BackgroundActivitySchedulesKey = new();

    /// <inheritdoc />
    public ScheduleBackgroundActivitiesMiddleware(WorkflowMiddlewareDelegate next, IBackgroundActivityScheduler backgroundActivityScheduler) : base(next)
    {
        _backgroundActivityScheduler = backgroundActivityScheduler;
    }

    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        // Invoke next middleware.
        await Next(context);

        // Get activities to schedule.
        if (context.TransientProperties.ContainsKey(BackgroundActivitySchedulesKey))
        {
            var scheduledActivities = (ICollection<ScheduledBackgroundActivity>)context.TransientProperties.GetValue(BackgroundActivitySchedulesKey)!;

            // Schedule activities.
            foreach (var scheduledBackgroundActivity in scheduledActivities)
                await _backgroundActivityScheduler.ScheduleAsync(scheduledBackgroundActivity, context.CancellationToken);
        }
    }
}