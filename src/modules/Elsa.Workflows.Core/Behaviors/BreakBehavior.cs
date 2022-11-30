using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.Signals;

namespace Elsa.Workflows.Core.Behaviors;

/// <summary>
/// Implements a "break" behavior that handles the <see cref="BreakSignal"/> signal.
/// </summary>
public class BreakBehavior : Behavior
{
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
            await workflowExecutionContext.CancelActivityAsync(descendant.Id);

        // Remove child activity execution contexts.
        var childActivityExecutionContexts = context.ReceiverActivityExecutionContext.GetChildren().ToList();
        context.ReceiverActivityExecutionContext.WorkflowExecutionContext.RemoveActivityExecutionContexts(childActivityExecutionContexts);

        // Mark this activity as completed.
        await context.ReceiverActivityExecutionContext.CompleteActivityAsync();
    }
}