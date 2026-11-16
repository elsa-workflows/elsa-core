using Elsa.Workflows.State;

namespace Elsa.Workflows.Exceptions;

/// <summary>
/// Raised in the workflow for an exception that an activity threw while it ran in the background.
/// </summary>
/// <remarks>
/// A background run executes the activity against a separate workflow execution context, so the original exception is
/// captured as an exception state and carried back when the workflow resumes. Incidents recorded for this exception
/// report the captured state, which keeps the original exception's message and stack trace.
/// </remarks>
public class BackgroundActivityException(ExceptionState exceptionState) : Exception(exceptionState.Message)
{
    /// <summary>
    /// The state of the exception that the activity threw in the background.
    /// </summary>
    public ExceptionState ExceptionState { get; } = exceptionState;

    /// <inheritdoc />
    public override string? StackTrace => ExceptionState.StackTrace;
}
