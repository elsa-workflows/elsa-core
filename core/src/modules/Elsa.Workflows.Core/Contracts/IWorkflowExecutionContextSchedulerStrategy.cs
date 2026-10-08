using Elsa.Workflows.Models;
using Elsa.Workflows.Options;

namespace Elsa.Workflows;

/// <summary>
/// Defines a strategy interface for scheduling activities within the context of a workflow execution.
/// </summary>
public interface IWorkflowExecutionContextSchedulerStrategy
{
    /// <summary>
    /// Schedules an <see cref="ActivityWorkItem"/> in the context of the provided workflow execution.
    /// </summary>
    /// <param name="context">The execution context of the workflow in which the activity is being scheduled.</param>
    /// <param name="activityNode">The node representing the activity to be scheduled.</param>
    /// <param name="owner">The <see cref="ActivityExecutionContext"/> that owns the scheduled activity.</param>
    /// <param name="options">Optional parameters for scheduling the work item.</param>
    /// <returns>The scheduled <see cref="ActivityWorkItem"/>.</returns>
    ActivityWorkItem Schedule(WorkflowExecutionContext context,
        ActivityNode activityNode,
        ActivityExecutionContext owner,
        ScheduleWorkOptions? options = null);
}