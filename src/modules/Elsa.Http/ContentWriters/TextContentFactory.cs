using System.Net.Mime;
using System.Text;

namespace Elsa.Http.ContentWriters;

/// <summary>
/// Creates a <see cref="StringContent"/> object for text/plain, text/richtext and text/html content types.
/// </summary>
public class TextContentFactory : IHttpContentFactory
{
    /// <inheritdoc />
    public IEnumerable<string> SupportedContentTypes => new[]
    {
        MediaTypeNames.Text.Plain,
        MediaTypeNames.Text.RichText,
        MediaTypeNames.Text.Html,
    };

    /// <inheritdoc />
    public HttpContent CreateHttpContent(object content, string contentType)
    {
        var text = content as string ?? content.ToString();

        if (string.IsNullOrWhiteSpace(contentType))
            contentType = MediaTypeNames.Text.Plain;
        
        return new StringContent(text!, Encoding.UTF8, contentType);
    }
}