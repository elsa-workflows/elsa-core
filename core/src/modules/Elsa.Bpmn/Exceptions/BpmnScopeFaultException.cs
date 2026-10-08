namespace Elsa.Bpmn.Exceptions;

/// <summary>
/// Thrown when the interpreter returns a <c>Fault</c> continuation for a BPMN scope: the scope failed
/// deterministically and cannot continue.
/// </summary>
/// <remarks>
/// A propagated work fault does not surface this way. That case is handled where it arrives — inside the scope's
/// <see cref="Elsa.Workflows.Signals.FaultSignal"/> handler, which leaves the signal alone so an enclosing scope or
/// the incident strategy takes it. This exception covers the remaining ways a scope can fault, such as a deadlock
/// the interpreter detects while routing a completion, where there is no signal in flight to leave alone.
/// </remarks>
public class BpmnScopeFaultException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BpmnScopeFaultException"/> class.
    /// </summary>
    /// <param name="code">The interpreter's stable fault code.</param>
    /// <param name="message">The interpreter's human-readable explanation.</param>
    public BpmnScopeFaultException(string code, string message) : base($"{code}: {message}")
    {
        Code = code;
    }

    /// <summary>
    /// The interpreter's stable fault code.
    /// </summary>
    public string Code { get; }
}
