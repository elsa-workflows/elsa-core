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

        // Remove child activity execution contexts.
        var childActivityExecutionContexts = context.ActivityExecutionContext.GetChildren().ToList();
        context.ActivityExecutionContext.WorkflowExecutionContext.RemoveActivityExecutionContexts(childActivityExecutionContexts);
        
        // Mark this activity as completed.
        await context.ActivityExecutionContext.CompleteActivityAsync();
    }
}