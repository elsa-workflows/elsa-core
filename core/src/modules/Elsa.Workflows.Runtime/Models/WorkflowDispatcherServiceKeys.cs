namespace Elsa.Workflows.Runtime;

/// <summary>
/// Keys for keyed <see cref="IWorkflowDispatcher"/> registrations.
/// </summary>
public static class WorkflowDispatcherServiceKeys
{
    /// <summary>
    /// The inner dispatcher that delivers dispatches without writing to the workflow dispatch outbox.
    /// </summary>
    public const string Inner = "Elsa:WorkflowDispatcher:Inner";
}
