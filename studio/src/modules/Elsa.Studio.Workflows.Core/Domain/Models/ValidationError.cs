namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// Represents a validation error.
/// </summary>
/// <param name="ErrorMessage"></param>
public record ValidationError(string ErrorMessage)
{
    /// <summary>
    /// Returns the error message.
    /// </summary>
    public override string ToString() => ErrorMessage;
}