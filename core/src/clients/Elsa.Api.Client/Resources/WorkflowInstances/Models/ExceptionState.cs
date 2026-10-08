using System.Text.Json.Serialization;

namespace Elsa.Api.Client.Resources.WorkflowInstances.Models;

/// <summary>
/// A simplified, serializable model representing an exception.
/// </summary>
public record ExceptionState(
    string Type,
    string Message,
    string? StackTrace,
    ExceptionState? InnerException)
{
    /// <summary>
    /// Gets privacy-safe structured exception metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionState"/> class.
    /// </summary>
    [JsonConstructor]
    public ExceptionState() : this(null!, null!, null, null)
    {
        
    }
}
