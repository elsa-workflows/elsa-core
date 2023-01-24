using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;

namespace Elsa.Http.ContentWriters;

/// <summary>
/// Creates a <see cref="HttpContent"/> object for application/json.
/// </summary>
public class JsonContentFactory : IHttpContentFactory
{
    private readonly List<string> _supportedContentTypes = new() { MediaTypeNames.Application.Json, "text/json" };

    /// <inheritdoc />
    public bool SupportsContentType(string contentType) => _supportedContentTypes.Contains(contentType);

    /// <inheritdoc />
    public HttpContent CreateHttpContent(object content, string contentType)
    {
        if (content is string s)
            return new StringContent(s, Encoding.UTF8, contentType);

        var mediaType = MediaTypeHeaderValue.Parse(contentType);
        return JsonContent.Create(content, mediaType);
    }
}