namespace Elsa.Http.Contracts;

/// <summary>
/// A strategy that reads a <see cref="HttpResponseMessage"/> of a given content type.
/// </summary>
public interface IHttpContentParser
{
    /// <summary>
    /// The priority of the parser as compared to other parsers.
    /// The higher the number, the higher priority.
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Returns a value indicating whether this reader supports the specified content type.
    /// </summary>
    bool GetSupportsContentType(string contentType);
    
    /// <summary>
    /// Reads the specified <see cref="stream"/> and returns a parsed object of the specified type. If no type is specified, a string is returned. 
    /// </summary>
    Task<object> ReadAsync(Stream content, Type? returnType, CancellationToken cancellationToken);
}