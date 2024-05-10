namespace Elsa.Workflows.Exceptions;

/// <summary>
/// Represents an exception that occurred during workflow execution.
/// </summary>
public class FaultException : Exception
{
    /// <inheritdoc />
    public FaultException(string code, string category, string kind, string? message) : base(message)
    {
        Code = code;
        Category = category;
        Kind = kind;
    }

    /// <summary>
    /// Code that identifies the fault type.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Category to categorize the fault. E.g. "HTTP", "Alteration", "Azure", etc.
    /// This is used to distinguish error codes between modules.
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Kind of fault. E.g. "System", "Business", "Integration", etc.
    /// </summary>
    public string Kind { get; }
}