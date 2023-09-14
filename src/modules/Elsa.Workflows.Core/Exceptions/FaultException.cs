namespace Elsa.Workflows.Core.Exceptions;

/// <summary>
/// Represents an exception that occurred during workflow execution.
/// </summary>
public class FaultException : Exception
{
    /// <inheritdoc />
    public FaultException(string? message) : base(message)
    {
    }
}