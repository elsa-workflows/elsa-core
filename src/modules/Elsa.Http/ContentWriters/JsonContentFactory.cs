using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace Elsa.Http.ContentWriters;

/// <summary>
/// Creates a <see cref="HttpContent"/> object for application/json.
/// </summary>
public class JsonContentFactory : IHttpContentFactory
{
    /// <inheritdoc />
    public IEnumerable<string> SupportedContentTypes => new[] { MediaTypeNames.Application.Json, "text/json" };

    /// <inheritdoc />
    [RequiresUnreferencedCode("The JsonSerializer type is not trim-compatible.")]
    public HttpContent CreateHttpContent(object content, string contentType)
    {
        var text = content as string ?? JsonSerializer.Serialize(content);

        if (string.IsNullOrWhiteSpace(contentType))
            contentType = MediaTypeNames.Application.Json;

        return new StringContent(text, Encoding.UTF8, contentType);
    }
}