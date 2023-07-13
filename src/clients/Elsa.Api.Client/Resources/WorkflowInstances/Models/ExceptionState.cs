using System.Text.Json.Serialization;

namespace Elsa.Api.Client.Resources.WorkflowInstances.Models;

/// <summary>
/// A simplified, serializable model representing an exception.
/// </summary>
public record ExceptionState(string Type, string Message, string? StackTrace, ExceptionState? InnerException)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionState"/> class.
    /// </summary>
    [JsonConstructor]
    public ExceptionState() : this(default!, default!, default, default)
    {
        
    }
}