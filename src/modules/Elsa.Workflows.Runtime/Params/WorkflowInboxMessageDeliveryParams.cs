namespace Elsa.Workflows.Runtime.Params;

/// <summary>
/// Options for delivering a workflow inbox message.
/// </summary>
public class WorkflowInboxMessageDeliveryParams
{
    /// <summary>
    /// Whether to dispatch the message to the workflow dispatcher or send immediately.
    /// </summary>
    public bool DispatchAsynchronously { get; set; } = true;
}