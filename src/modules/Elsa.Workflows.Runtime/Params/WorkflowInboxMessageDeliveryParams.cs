namespace Elsa.Workflows.Runtime.Params;

/// <summary>
/// Options for delivering a workflow inbox message.
/// </summary>
[Obsolete("This type is obsolete. Use the new IBookmarkQueue service instead.")]
public class WorkflowInboxMessageDeliveryParams
{
    /// <summary>
    /// Whether to dispatch the message to the workflow dispatcher or send immediately.
    /// </summary>
    public bool DispatchAsynchronously { get; set; } = true;
}