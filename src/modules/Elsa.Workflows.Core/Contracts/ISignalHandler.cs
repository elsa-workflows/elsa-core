namespace Elsa.Workflows.Core.Contracts;

public interface ISignalHandler
{
    ValueTask HandleSignalAsync(object signal, SignalContext context);
}

public class SignalContext
{
    public SignalContext(ActivityExecutionContext receiverActivityExecutionContext, ActivityExecutionContext senderActivityExecutionContext, CancellationToken cancellationToken)
    {
        ReceiverActivityExecutionContext = receiverActivityExecutionContext;
        SenderActivityExecutionContext = senderActivityExecutionContext;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// The <see cref="ActivityExecutionContext"/> receiving the signal.
    /// </summary>
    public ActivityExecutionContext ReceiverActivityExecutionContext { get; init; }
    
    /// <summary>
    /// The <see cref="ActivityExecutionContext"/> sending the signal.
    /// </summary>
    public ActivityExecutionContext SenderActivityExecutionContext { get; init; }
    
    /// <summary>
    /// Returns true if the receiver is the same as the sender.
    /// </summary>
    public bool IsSelf => SenderActivityExecutionContext.Activity == ReceiverActivityExecutionContext.Activity;
    
    public CancellationToken CancellationToken { get; init; }
    internal bool StopPropagationRequested { get; private set; }

    /// <summary>
    /// Stops the signal from propagating further up the activity execution context hierarchy.
    /// </summary>
    public void StopPropagation() => StopPropagationRequested = true;
}