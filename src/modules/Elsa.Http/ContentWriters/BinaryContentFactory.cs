using System.Net.Mime;

namespace Elsa.Http.ContentWriters;

/// <summary>
/// Creates a <see cref="HttpContent"/> object for application/octet-stream.
/// </summary>
public class BinaryContentFactory : IHttpContentFactory
{
    /// <inheritdoc />
    public IEnumerable<string> SupportedContentTypes => new[] { MediaTypeNames.Application.Octet };

    /// <inheritdoc />
    public HttpContent CreateHttpContent(object content, string contentType)
    {
        return content switch
        {
            byte[] bytes => new ByteArrayContent(bytes),
            Stream stream => new StreamContent(stream),
            _ => throw new NotSupportedException($"Content of type {content.GetType()} is not supported.")
        };
    }
}