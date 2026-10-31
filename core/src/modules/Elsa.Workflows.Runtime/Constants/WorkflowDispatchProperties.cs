namespace Elsa.Workflows.Runtime;

/// <summary>
/// Workflow-instance property names written by dispatching activities and read by their resume and cancel handlers.
/// </summary>
internal static class WorkflowDispatchProperties
{
    /// <summary>
    /// Marks a child as one the dispatching activity is waiting to complete.
    /// </summary>
    public const string WaitForCompletion = "WaitForCompletion";

    /// <summary>
    /// The activity instance id that is waiting for this child. Used to cancel only that activity's children.
    /// </summary>
    public const string WaitingActivityInstanceId = "WaitingActivityInstanceId";
}
