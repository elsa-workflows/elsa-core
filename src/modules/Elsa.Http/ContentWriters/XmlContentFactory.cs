using System.Net.Mime;
using System.Text;
using System.Xml.Serialization;

namespace Elsa.Http.ContentWriters;

/// <summary>
/// Creates a <see cref="HttpContent"/> object for application/json.
/// </summary>
public class XmlContentFactory : IHttpContentFactory
{
    private readonly List<string> _supportedContentTypes = new() { MediaTypeNames.Application.Xml, MediaTypeNames.Text.Xml };

    /// <inheritdoc />
    public bool SupportsContentType(string contentType) => _supportedContentTypes.Contains(contentType);

    /// <inheritdoc />
    public HttpContent CreateHttpContent(object content, string contentType)
    {
        var text = content as string ?? Serialize(content);
        return new StringContent(text, Encoding.UTF8, contentType);
    }
    
    private string Serialize(object value)
    {
        using var writer = new StringWriter();
        new XmlSerializer(value.GetType()).Serialize(writer, value);
        return writer.ToString();
    }
}