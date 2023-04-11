using Elsa.Workflows.Runtime.Models.Requests;

namespace Elsa.Scheduling.Contracts;

/// <summary>
/// A contract for scheduling workflows to execute at a specific future instant. Can be used to implement a custom scheduler, e.g. using Quartz.NET and Hangfire.
/// </summary>
public interface IWorkflowScheduler
{
    /// <summary>
    /// Schedules a workflow request to be executed at the specified time.
    /// </summary>
    /// <param name="taskName">The name of the task to schedule.</param>
    /// <param name="request">The workflow request to schedule.</param>
    /// <param name="at">The time at which the workflow should be executed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask ScheduleAtAsync(string taskName, DispatchWorkflowDefinitionRequest request, DateTimeOffset at, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Schedules a workflow request to be executed at the specified time.
    /// </summary>
    /// <param name="taskName">The name of the task to schedule.</param>
    /// <param name="request">The workflow request to schedule.</param>
    /// <param name="at">The time at which the workflow should be executed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask ScheduleAtAsync(string taskName, DispatchWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a workflow request to be executed at the specified time.
    /// </summary>
    /// <param name="taskName">The name of the task to schedule.</param>
    /// <param name="request">The workflow request to schedule.</param>
    /// <param name="startAt">The time at which the first execution should occur.</param>
    /// <param name="interval">The interval at which the workflow should be executed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask ScheduleRecurringAsync(string taskName, DispatchWorkflowDefinitionRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Schedules a workflow request to be executed at the specified time.
    /// </summary>
    /// <param name="taskName">The name of the task to schedule.</param>
    /// <param name="request">The workflow request to schedule.</param>
    /// <param name="startAt">The time at which the first execution should occur.</param>
    /// <param name="interval">The interval at which the workflow should be executed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask ScheduleRecurringAsync(string taskName, DispatchWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a workflow request to be executed at the specified time.
    /// </summary>
    /// <param name="taskName">The name of the task to schedule.</param>
    /// <param name="request">The workflow request to schedule.</param>
    /// <param name="cronExpression">The cron expression to use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask ScheduleCronAsync(string taskName, DispatchWorkflowDefinitionRequest request, string cronExpression, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Schedules a workflow request to be executed at the specified time.
    /// </summary>
    /// <param name="taskName">The name of the task to schedule.</param>
    /// <param name="request">The workflow request to schedule.</param>
    /// <param name="cronExpression">The cron expression to use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask ScheduleCronAsync(string taskName, DispatchWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clears the schedule for the specified task.
    /// </summary>
    /// <param name="taskName">The name of the task to unschedule.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask UnscheduleAsync(string taskName, CancellationToken cancellationToken = default);
}