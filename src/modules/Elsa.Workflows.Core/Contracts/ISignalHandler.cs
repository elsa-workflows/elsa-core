namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Handles signals.
/// </summary>
public interface ISignalHandler
{
    /// <summary>
    /// Captures a signal.
    /// </summary>
    ValueTask CaptureSignalAsync(object signal, SignalContext context);
    
    /// <summary>
    /// Receives a signal.
    /// </summary>
    ValueTask ReceiveSignalAsync(object signal, SignalContext context);
}