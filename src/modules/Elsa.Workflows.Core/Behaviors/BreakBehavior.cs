using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Signals;

namespace Elsa.Workflows.Core.Behaviors;

/// <summary>
/// Implements a "break" behavior that handles the <see cref="BreakSignal"/> signal.
/// </summary>
public class BreakBehavior : Behavior
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public BreakBehavior(IActivity owner) : base(owner)
    {
        OnSignalReceived<BreakSignal>(OnBreakAsync);
    }

    private async ValueTask OnBreakAsync(BreakSignal signal, SignalContext context)
    {
        // Prevent bubbling.
        context.StopPropagation();
        
        // Cancel each descendant to clear bookmarks and cancel jobs etc.
        var descendants = context.ReceiverActivityExecutionContext.ActivityNode.Descendants().Select(x => x.Activity).ToList();
        var workflowExecutionContext = context.SenderActivityExecutionContext.WorkflowExecutionContext;

        foreach (var descendant in descendants) 
            await workflowExecutionContext.CancelActivityAsync(descendant);

        // Remove child activity execution contexts.
        await context.ReceiverActivityExecutionContext.RemoveChildrenAsync();

        // Mark this activity as completed.
        await context.ReceiverActivityExecutionContext.CompleteActivityAsync();
    }
}