namespace Elsa.Workflows;

/// <summary>
/// Handles signals.
/// </summary>
public interface ISignalHandler
{
    /// <summary>
    /// Receives a signal.
    /// </summary>
    ValueTask ReceiveSignalAsync(object signal, SignalContext context);
}