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
        OnSignalReceived<CompleteCompositeSignal>(OnCompleteCompositeAsync);
    }

    private async ValueTask OnCompleteCompositeAsync(CompleteCompositeSignal signal, SignalContext context)
    {
        // Cancel each descendant to clear bookmarks and cancel jobs etc.
        await CancelDescendantsAsync(context);

        // Remove child activity execution contexts.
        await context.ReceiverActivityExecutionContext.RemoveChildrenAsync();

        // Mark this activity as completed.
        await context.ReceiverActivityExecutionContext.CompleteActivityAsync();
    }

    private async ValueTask OnBreakAsync(BreakSignal signal, SignalContext context)
    {
        // Prevent bubbling.
        context.StopPropagation();
        
        // Cancel each descendant to clear bookmarks and cancel jobs etc.
        await CancelDescendantsAsync(context);

        // Remove child activity execution contexts.
        await context.ReceiverActivityExecutionContext.RemoveChildrenAsync();

        // Mark this activity as completed.
        await context.ReceiverActivityExecutionContext.CompleteActivityAsync();
    }
    
    private async Task CancelDescendantsAsync(SignalContext context)
    {
        var senderActivity = context.SenderActivityExecutionContext.Activity;
        var descendants = context.ReceiverActivityExecutionContext.ActivityNode.Descendants().Select(x => x.Activity).Where(x => x.Id != senderActivity.Id).ToList();
        var workflowExecutionContext = context.ReceiverActivityExecutionContext.WorkflowExecutionContext;

        foreach (var descendant in descendants) 
            await workflowExecutionContext.CancelActivityAsync(descendant);
    }
}