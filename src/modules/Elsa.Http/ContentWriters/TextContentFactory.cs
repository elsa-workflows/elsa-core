using System.Net.Mime;
using System.Text;

namespace Elsa.Http.ContentWriters;

/// <summary>
/// Creates a <see cref="StringContent"/> object for application/json, application/xml, text/json and text/xml content types.
/// </summary>
public class TextContentFactory : IHttpContentFactory
{
    private readonly List<string> _supportedContentTypes = new()
    {
        MediaTypeNames.Text.Plain,
        MediaTypeNames.Text.Xml,
        MediaTypeNames.Text.RichText,
        MediaTypeNames.Text.Html,
    };

    /// <inheritdoc />
    public bool SupportsContentType(string contentType) => _supportedContentTypes.Contains(contentType);

    /// <inheritdoc />
    public HttpContent CreateHttpContent(object content, string contentType)
    {
        var text = content as string ?? "";
        return new StringContent(text, Encoding.UTF8, contentType);
    }
}