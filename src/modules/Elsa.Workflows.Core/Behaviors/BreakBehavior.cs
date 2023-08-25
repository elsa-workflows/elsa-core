using Elsa.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Signals;

namespace Elsa.Workflows.Core.Behaviors;

/// <summary>
/// Implements a "break" behavior that handles the <see cref="BreakSignal"/> signal.
/// Stops propagation of the signal, which is useful for looping activities such as <see cref="While"/>, <see cref="For"/> ans <see cref="ForEach"/>.
/// </summary>
public class BreakBehavior : Behavior
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public BreakBehavior(IActivity owner) : base(owner)
    {
        OnSignalReceived<BreakSignal>(OnBreak);
        OnSignalReceived<CompleteCompositeSignal>(OnCompleteCompositeAsync);
    }

    private async ValueTask OnCompleteCompositeAsync(CompleteCompositeSignal signal, SignalContext context)
    {
        // Cancel each descendant to clear bookmarks and cancel jobs etc.
        await CancelDescendantsAsync(context);

        // Mark this activity as completed.
        await context.ReceiverActivityExecutionContext.CompleteActivityAsync();
    }

    private void OnBreak(BreakSignal signal, SignalContext context)
    {
        // Prevent bubbling.
        context.StopPropagation();
        
        // Set the IsBreaking property to true.
        context.ReceiverActivityExecutionContext.SetProperty("IsBreaking", true);
    }
    
    private async Task CancelDescendantsAsync(SignalContext context)
    {
        await context.ReceiverActivityExecutionContext.CancelActivityAsync();
    }
}