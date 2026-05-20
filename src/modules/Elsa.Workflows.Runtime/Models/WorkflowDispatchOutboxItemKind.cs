namespace Elsa.Workflows.Runtime.Models;

/// <summary>
/// The supported workflow dispatch outbox command kinds.
/// </summary>
public enum WorkflowDispatchOutboxItemKind
{
    /// <summary>
    /// Dispatches a workflow definition.
    /// </summary>
    WorkflowDefinition,

    /// <summary>
    /// Dispatches an existing workflow instance.
    /// </summary>
    WorkflowInstance,

    /// <summary>
    /// Dispatches workflows by trigger stimulus.
    /// </summary>
    TriggerWorkflows,

    /// <summary>
    /// Dispatches workflows by resume stimulus.
    /// </summary>
    ResumeWorkflows
}
