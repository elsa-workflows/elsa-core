using Elsa.Workflows.Runtime.Messages;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// A high-level service for triggering user actions against suspended <see cref="Activities.RunTask"/> activities.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ITaskReporter"/> is the low-level interface for resuming a <see cref="Activities.RunTask"/> when the
/// caller already knows the unique <c>TaskId</c> that was generated at dispatch time.  <see cref="IUserTaskService"/>
/// complements it by allowing callers to target a task by <em>workflow identity</em> (workflow-instance ID, activity-
/// instance ID, or correlation ID) rather than by the internal task ID.
/// </para>
/// <para>
/// This is especially important when a <c>Fork</c> activity creates several parallel branches that each contain a
/// <see cref="Activities.RunTask"/> with the same <c>TaskName</c>: without an <c>ActivityInstanceId</c> the caller
/// would be unable to distinguish which branch to resume.
/// </para>
/// </remarks>
public interface IUserTaskService
{
    /// <summary>
    /// Synchronously resumes all <see cref="Activities.RunTask"/> bookmarks that match <paramref name="taskAction"/>,
    /// executing the workflow continuation in-process before returning.
    /// </summary>
    /// <param name="taskAction">The trigger parameters, including the action payload and optional scope filters.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>One <see cref="RunWorkflowInstanceResponse"/> per workflow instance that was resumed.</returns>
    Task<IEnumerable<RunWorkflowInstanceResponse>> ExecuteUserActionAsync(
        TriggerUserAction taskAction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously enqueues a resume request for all <see cref="Activities.RunTask"/> bookmarks that match
    /// <paramref name="taskAction"/>, returning as soon as the request has been queued.
    /// </summary>
    /// <param name="taskAction">The trigger parameters, including the action payload and optional scope filters.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task DispatchUserActionAsync(
        TriggerUserAction taskAction,
        CancellationToken cancellationToken = default);
}
