namespace Elsa.Workflows.Exceptions;

/// An exception that occurs during data processing.
public class DataProcessingException(bool isUkViolation, string message, Exception exception) : Exception(message, exception)
{
    /// Gets a value indicating whether the exception is a Unique Key violation.
    public bool IsUkViolation { get; } = isUkViolation;
}
