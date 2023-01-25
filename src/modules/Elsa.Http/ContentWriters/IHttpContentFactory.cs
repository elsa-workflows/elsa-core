namespace Elsa.Http.ContentWriters;

/// <summary>
/// Creates a concrete <see cref="HttpContent"/> instance based on the specified content type.
/// </summary>
public interface IHttpContentFactory
{
    /// <summary>
    /// Returns a value indicating whether this factory supports the specified content type.
    /// </summary>
    bool SupportsContentType(string contentType);
    
    /// <summary>
    /// Creates a concrete <see cref="HttpContent"/> derivative based on the specified content type.
    /// </summary>
    HttpContent CreateHttpContent(object content, string contentType);
}