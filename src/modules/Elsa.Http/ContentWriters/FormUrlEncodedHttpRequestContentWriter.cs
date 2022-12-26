using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Http.Constants;

namespace Elsa.Http.ContentWriters;

/// <summary>
/// A content writer that writes content in the application/x-www-form-urlencoded format. 
/// </summary>
public class FormUrlEncodedHttpRequestContentWriter : IHttpRequestContentWriter
{
    private readonly List<string> _supportedContentTypes = new() {MimeTypes.ApplicationWwwFormUrlEncoded};

    /// <inheritdoc />
    public bool SupportsContentType(string contentType) => _supportedContentTypes.Contains(contentType);

    /// <inheritdoc />
    public HttpContent GetContent<T>( T content, string? contentType = null) => new FormUrlEncodedContent(GetContentAsDictionary(content));

    private static Dictionary<string, string> GetContentAsDictionary<TType>(TType body) =>
        (body is string || body is JsonObject ?
            JsonSerializer.Deserialize<Dictionary<string, string>>(JsonSerializer.Serialize(body)) :
            (Dictionary<string, string>)Convert.ChangeType(body, typeof(Dictionary<string, string>))!)!;
}