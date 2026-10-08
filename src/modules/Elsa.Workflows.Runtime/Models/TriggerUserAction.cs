namespace Elsa.Workflows.Runtime;

/// <summary>
/// Encapsulates the parameters required to trigger a user action against a suspended <see cref="Activities.RunTask"/> activity.
/// </summary>
/// <param name="Action">
/// The action (or result payload) being performed by the user. This value is passed as the task result when the
/// target <see cref="Activities.RunTask"/> activity is resumed.
/// </param>
/// <param name="WorkflowInstanceId">
/// Optionally narrows the search to a specific workflow instance.
/// </param>
/// <param name="ActivityInstanceId">
/// Optionally targets a specific activity instance. Useful when a Fork contains multiple <see cref="Activities.RunTask"/>
/// activities with the same task name; supplying this ID ensures only the intended activity is resumed.
/// </param>
/// <param name="CorrelationId">
/// Optionally narrows the search to bookmarks whose workflow instance carries this correlation ID.
/// </param>
public record TriggerUserAction(
    object Action,
    string? WorkflowInstanceId = default,
    string? ActivityInstanceId = default,
    string? CorrelationId = default);
