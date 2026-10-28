namespace Elsa.Http.ContentWriters;

/// <summary>
/// A content writer that writes content in the application/x-www-form-urlencoded format.
/// </summary>
/// <remarks>
/// Accepts a dictionary, a JSON object, a JSON object string, or any other object. Each property becomes one form field.
/// </remarks>
public class FormUrlEncodedHttpContentFactory : IHttpContentFactory
{
    /// <inheritdoc />
    public IEnumerable<string> SupportedContentTypes => ["application/x-www-form-urlencoded"];

    /// <inheritdoc />
    public HttpContent CreateHttpContent(object content, string? contentType = null) => new FormUrlEncodedContent(FormContentReader.ReadStrings(content));
}
