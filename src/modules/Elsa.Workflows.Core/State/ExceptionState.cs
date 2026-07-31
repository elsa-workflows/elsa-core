using System.Text.Json.Serialization;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Elsa.Workflows.State;

/// <summary>
/// A simplified, serializable model representing an exception.
/// </summary>
public record ExceptionState(
    Type Type,
    string Message,
    string? StackTrace,
    ExceptionState? InnerException,
    IReadOnlyDictionary<string, string>? Metadata = null)
{
    /// <summary>
    /// Constructor
    /// </summary>
    [JsonConstructor]
    public ExceptionState() : this(default!, default!, default, default, default)
    {
        
    }
    
    /// <summary>
    /// Creates a new <see cref="ExceptionState"/> from the specified exception. 
    /// </summary>
    public static ExceptionState? FromException(Exception? ex)
    {
        if (ex == null)
            return null;

        var metadataProvider = ex as ISafeExceptionMetadataProvider;
        var metadata = metadataProvider?.GetSafeMetadata();
        var innerException = metadataProvider == null ? FromException(ex.InnerException) : null;

        return new ExceptionState(ex.GetType(), ex.Message, ex.StackTrace, innerException, metadata);
    }
}
